import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams, Link } from 'react-router-dom';
import { useMemo, useState } from 'react';
import { matchesApi, videosApi } from '../services/api';
import { VideoPlayer } from '../components/VideoPlayer';

export function MatchDetailPage() {
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
