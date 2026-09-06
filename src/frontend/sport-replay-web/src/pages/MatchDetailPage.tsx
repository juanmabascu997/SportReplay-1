import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams, Link } from 'react-router-dom';
import { useEffect, useMemo, useState } from 'react';
import { clubsApi, matchesApi, paymentsApi, profileApi, videosApi } from '../services/api';
import { VideoPlayer } from '../components/VideoPlayer';
import { useAuth } from '../hooks/useAuth';
import { formatMoney } from '../utils/format';
import { isStaff } from '../utils/roles';

export function MatchDetailPage() {
  const { user } = useAuth();
  if (isStaff(user?.role)) {
    return <StaffMatchDetail />;
  }
  return <PlayerMatchDetail />;
}

function PlayerMatchDetail() {
  const { id = '' } = useParams();
  const [phone, setPhone] = useState('');
  const [checkout, setCheckout] = useState(false);
  const [error, setError] = useState('');

  const match = useQuery({ queryKey: ['match', id], queryFn: () => matchesApi.get(id) });
  const recordings = useQuery({ queryKey: ['recordings', id], queryFn: () => matchesApi.recordings(id) });
  const prices = useQuery({
    queryKey: ['prices', match.data?.clubId],
    queryFn: () => clubsApi.prices(match.data!.clubId),
    enabled: !!match.data?.clubId
  });
  const profile = useQuery({ queryKey: ['profile'], queryFn: profileApi.get });
  const ready = recordings.data?.find((r) => r.status === 3) ?? recordings.data?.[0];
  const stream = useQuery({
    queryKey: ['recording-stream', ready?.id],
    queryFn: () => videosApi.recordingStream(ready!.id),
    enabled: !!ready?.id
  });

  useEffect(() => {
    if (profile.data?.phone) {
      setPhone((current) => current || profile.data!.phone || '');
    }
  }, [profile.data?.phone]);

  const pay = useMutation({
    mutationFn: async () => {
      const request = await videosApi.requestWhatsApp({ matchId: id, phoneNumber: phone.trim() });
      return paymentsApi.create({
        matchId: id,
        videoRequestId: request.id,
        productType: 2
      });
    },
    onSuccess: (payment) => {
      if (payment.initPoint) {
        window.location.href = payment.initPoint;
      }
    }
  });

  const price = prices.data?.fullMatchPrice ?? 4500;
  const currency = prices.data?.currency ?? 'ARS';

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

      {ready ? (
        <>
          <VideoPlayer src={stream.data?.url} kind={stream.data?.kind} />
          {stream.isError && <p className="text-sm text-danger">No se pudo cargar la previsualizacion.</p>}
        </>
      ) : (
        <div className="card">No hay grabacion lista para previsualizar.</div>
      )}

      {ready && !checkout && (
        <div className="card space-y-3">
          <p>Si queres descargar o compartir el partido, te lo enviamos por WhatsApp despues del pago.</p>
          <button className="btn-primary w-full" onClick={() => setCheckout(true)}>
            Descargar y compartir
          </button>
        </div>
      )}

      {checkout && (
        <div className="card space-y-3">
          <p className="font-semibold">Partido completo</p>
          <p className="font-display text-3xl text-accent">{formatMoney(price, currency)}</p>
          <p className="text-sm text-slate-400">
            Al confirmar, vas a Mercado Pago. Cuando el pago se aprueba, enviamos el video a WhatsApp.
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
            disabled={pay.isPending}
            onClick={() => {
              if (phone.trim().length < 8) {
                setError('Ingresa un telefono de WhatsApp valido.');
                return;
              }
              setError('');
              pay.mutate();
            }}
          >
            {pay.isPending ? 'Redirigiendo...' : 'Pagar con Mercado Pago'}
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
  const recordingId = recordings.data?.[0]?.id;
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
