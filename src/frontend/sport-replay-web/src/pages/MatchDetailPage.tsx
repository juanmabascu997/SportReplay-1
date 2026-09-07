import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams, Link } from 'react-router-dom';
import { useEffect, useMemo, useState } from 'react';
import { clubsApi, matchesApi, paymentsApi, profileApi, videosApi } from '../services/api';
import { VideoPlayer } from '../components/VideoPlayer';
import { useAuth } from '../hooks/useAuth';
import { formatMoney } from '../utils/format';
import { isStaff } from '../utils/roles';
import { isUsableRecording } from '../utils/recordings';

export function MatchDetailPage() {
  const { user } = useAuth();
  if (isStaff(user?.role)) {
    return <StaffMatchDetail />;
  }
  return <PlayerMatchDetail />;
}

function PlayerMatchDetail() {
  const { id = '' } = useParams();
  const qc = useQueryClient();
  const [start, setStart] = useState(0);
  const [end, setEnd] = useState(30);
  const [phone, setPhone] = useState('');
  const [selectedClipId, setSelectedClipId] = useState('');
  const [error, setError] = useState('');

  const match = useQuery({ queryKey: ['match', id], queryFn: () => matchesApi.get(id) });
  const recordings = useQuery({ queryKey: ['recordings', id], queryFn: () => matchesApi.recordings(id) });
  const clips = useQuery({
    queryKey: ['clips', id],
    queryFn: () => matchesApi.clips(id),
    refetchInterval: (query) => (query.state.data?.some((clip) => clip.status === 1) ? 2000 : false)
  });
  const prices = useQuery({
    queryKey: ['prices', match.data?.clubId],
    queryFn: () => clubsApi.prices(match.data!.clubId),
    enabled: !!match.data?.clubId
  });
  const profile = useQuery({ queryKey: ['profile'], queryFn: profileApi.get });
  const recording = recordings.data?.find(isUsableRecording) ?? recordings.data?.[0];
  const stream = useQuery({
    queryKey: ['recording-stream', recording?.id],
    queryFn: () => videosApi.recordingStream(recording!.id),
    enabled: !!recording?.id,
    retry: false
  });

  useEffect(() => {
    if (profile.data?.phone) {
      setPhone((current) => current || profile.data!.phone || '');
    }
  }, [profile.data?.phone]);

  const duration = Math.max(0, end - start);
  const create = useMutation({
    mutationFn: () =>
      videosApi.createClip({
        matchId: id,
        recordingId: recording!.id,
        startTime: toTime(start),
        endTime: toTime(end)
      }),
    onSuccess: (created) => {
      setSelectedClipId(created.clipId);
      qc.invalidateQueries({ queryKey: ['clips', id] });
    }
  });

  const selectedClip = clips.data?.find((c) => c.id === selectedClipId) ?? clips.data?.find((c) => c.status === 2);
  const clipReady = (selectedClip?.status ?? 0) === 2;
  const pay = useMutation({
    mutationFn: async () => {
      const clipId = selectedClip?.id;
      if (!clipId) {
        throw new Error('Clip required');
      }
      const request = await videosApi.requestWhatsApp({
        matchId: id,
        videoClipId: clipId,
        phoneNumber: phone.trim()
      });
      return paymentsApi.create({
        videoClipId: clipId,
        videoRequestId: request.id,
        productType: 1
      });
    },
    onSuccess: (payment) => {
      if (payment.initPoint) {
        window.location.href = payment.initPoint;
      }
    }
  });

  const price = prices.data?.clipPrice ?? 1500;
  const currency = prices.data?.currency ?? 'ARS';
  const timeline = useMemo(() => Array.from({ length: 13 }, (_, i) => i * 5), []);

  return (
    <div className="mx-auto max-w-2xl space-y-5 pb-16">
      <Link className="text-sm text-accent" to="/buscar">Volver a la busqueda</Link>
      <div>
        <h1 className="font-display text-3xl">{match.data?.title ?? 'Partido'}</h1>
        <p className="text-slate-400">
          {match.data?.clubName} · {match.data?.courtName}
        </p>
        {match.data?.startTime && (
          <p className="text-sm text-slate-500">{new Date(match.data.startTime).toLocaleString()}</p>
        )}
      </div>

      {recording ? (
        <>
          <VideoPlayer src={stream.data?.url} kind={stream.data?.kind} />
          {stream.isError && (
            <p className="text-sm text-slate-400">La previsualizacion no esta disponible, pero podes crear un clip de esta grabacion.</p>
          )}
        </>
      ) : (
        <div className="card">Este partido no tiene grabacion.</div>
      )}

      {recording && (
        <div className="card space-y-3">
          <p className="font-semibold">Crear clip</p>
          <p className="text-sm text-slate-400">Usa la grabacion existente. Elegi el tramo y paga para recibirlo por WhatsApp.</p>
          <div className="flex gap-1 overflow-x-auto">
            {timeline.map((t) => (
              <button
                key={t}
                className={`min-w-10 rounded-lg px-2 py-6 text-xs ${t >= start && t <= end ? 'bg-accent text-ink' : 'bg-ink'}`}
                onClick={() => setStart(t)}
              >
                {t}m
              </button>
            ))}
          </div>
          <div className="grid grid-cols-2 gap-3">
            <label className="text-sm">Inicio (seg)
              <input className="input" type="number" value={start} onChange={(e) => setStart(Number(e.target.value))} />
            </label>
            <label className="text-sm">Fin (seg)
              <input className="input" type="number" value={end} onChange={(e) => setEnd(Number(e.target.value))} />
            </label>
          </div>
          <p>Duracion: {duration}s {duration > 60 ? '(max 60s)' : ''}</p>
          <button
            className="btn-primary w-full"
            disabled={!recording || duration <= 0 || duration > 60 || create.isPending}
            onClick={() => create.mutate()}
          >
            {create.isPending ? 'Creando clip...' : 'Crear clip'}
          </button>
          {create.data && <p className="text-accent">Clip {create.data.clipId} en proceso. Cuando este listo podes pagarlo.</p>}
        </div>
      )}

      {clips.data?.map((clip) => (
        <div key={clip.id} className={`card space-y-3 ${selectedClip?.id === clip.id ? 'border-accent' : ''}`}>
          <div className="flex items-center justify-between">
            <div>
              <p>Clip {clip.durationSeconds}s</p>
              <p className="text-xs text-slate-400">{clip.status === 2 ? 'Listo' : `Estado ${clip.status}`}</p>
            </div>
            <button className="btn-ghost" onClick={() => setSelectedClipId(clip.id)}>
              {selectedClip?.id === clip.id ? 'Seleccionado' : 'Elegir'}
            </button>
          </div>
        </div>
      ))}

      {recording && (
        <div className="card space-y-3">
          <p className="font-semibold">Pagar y enviar por WhatsApp</p>
          <p className="font-display text-3xl text-accent">{formatMoney(price, currency)}</p>
          <p className="text-sm text-slate-400">
            Al confirmar, vas a Mercado Pago. Cuando el pago se aprueba, enviamos el clip a WhatsApp.
          </p>
          <label className="block text-sm">
            WhatsApp (con codigo de pais)
            <input
              className="input mt-1"
              placeholder="54911..."
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
            />
          </label>
          {(error || pay.isError) && (
            <p className="text-sm text-danger">{error || 'No se pudo iniciar el pago.'}</p>
          )}
          <button
            className="btn-primary w-full"
            disabled={pay.isPending || !clipReady}
            onClick={() => {
              if (phone.trim().length < 8) {
                setError('Ingresa un telefono de WhatsApp valido.');
                return;
              }
              setError('');
              pay.mutate();
            }}
          >
            {pay.isPending ? 'Redirigiendo...' : clipReady ? 'Pagar con Mercado Pago' : 'Elegi o crea un clip listo'}
          </button>
        </div>
      )}
    </div>
  );
}

function StaffMatchDetail() {
  const { id = '' } = useParams();
  const qc = useQueryClient();
  const match = useQuery({ queryKey: ['match', id], queryFn: () => matchesApi.get(id) });
  const recordings = useQuery({ queryKey: ['recordings', id], queryFn: () => matchesApi.recordings(id) });
  const clips = useQuery({ queryKey: ['clips', id], queryFn: () => matchesApi.clips(id) });
  const [start, setStart] = useState(0);
  const [end, setEnd] = useState(30);
  const duration = Math.max(0, end - start);
  const recordingId = recordings.data?.find(isUsableRecording)?.id ?? recordings.data?.[0]?.id;
  const create = useMutation({
    mutationFn: () =>
      videosApi.createClip({
        matchId: id,
        recordingId: recordingId!,
        startTime: toTime(start),
        endTime: toTime(end)
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['clips', id] })
  });

  const timeline = useMemo(() => Array.from({ length: 13 }, (_, i) => i * 5), []);

  return (
    <div className="space-y-5 pb-16">
      <h1 className="font-display text-3xl">{match.data?.title ?? 'Partido'}</h1>
      <p className="text-slate-400">
        {match.data?.clubName} · {match.data?.courtName}
      </p>
      <VideoPlayer src={clips.data?.[0]?.streamUrl} kind="mp4" />
      <div className="card space-y-3">
        <p className="font-semibold">Timeline</p>
        <div className="flex gap-1 overflow-x-auto">
          {timeline.map((t) => (
            <button key={t} className={`min-w-10 rounded-lg px-2 py-6 text-xs ${t >= start && t <= end ? 'bg-accent text-ink' : 'bg-ink'}`} onClick={() => setStart(t)}>
              {t}m
            </button>
          ))}
        </div>
        <div className="grid grid-cols-2 gap-3">
          <label className="text-sm">Inicio (seg)
            <input className="input" type="number" value={start} onChange={(e) => setStart(Number(e.target.value))} />
          </label>
          <label className="text-sm">Fin (seg)
            <input className="input" type="number" value={end} onChange={(e) => setEnd(Number(e.target.value))} />
          </label>
        </div>
        <p>Duracion: {duration}s {duration > 60 ? '(max 60s)' : ''}</p>
        <button className="btn-primary" disabled={!recordingId || duration <= 0 || duration > 60} onClick={() => create.mutate()}>
          Crear clip
        </button>
        {create.data && <p className="text-accent">Clip {create.data.clipId} en proceso</p>}
      </div>
      {clips.data?.map((clip) => (
        <div key={clip.id} className="card flex items-center justify-between">
          <div>
            <p>Clip {clip.durationSeconds}s</p>
            <p className="text-xs text-slate-400">Estado {clip.status}</p>
          </div>
          <Link className="btn-primary" to={`/videos/${clip.id}`}>Ver</Link>
        </div>
      ))}
    </div>
  );
}

function toTime(seconds: number) {
  const h = Math.floor(seconds / 3600).toString().padStart(2, '0');
  const m = Math.floor((seconds % 3600) / 60).toString().padStart(2, '0');
  const s = Math.floor(seconds % 60).toString().padStart(2, '0');
  return `${h}:${m}:${s}`;
}
