import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { clubsApi, matchesApi } from '../services/api';

export function MatchesPage() {
  const clubs = useQuery({ queryKey: ['clubs'], queryFn: clubsApi.list });
  const [clubId, setClubId] = useState('');
  const [courtId, setCourtId] = useState('');
  const [date, setDate] = useState('');
  const [time, setTime] = useState('');
  const courts = useQuery({ queryKey: ['courts', clubId], queryFn: () => clubsApi.courts(clubId), enabled: !!clubId });
  const matches = useQuery({
    queryKey: ['matches', clubId, courtId, date, time],
    queryFn: () =>
      matchesApi.search({
        clubId: clubId || undefined,
        courtId: courtId || undefined,
        date: date || undefined,
        time: time || undefined
      })
  });

  return (
    <div className="space-y-4 pb-16">
      <h1 className="font-display text-3xl">Partidos</h1>
      <div className="card grid gap-3 md:grid-cols-4">
        <select className="input" value={clubId} onChange={(e) => { setClubId(e.target.value); setCourtId(''); }}>
          <option value="">Club</option>
          {clubs.data?.map((c) => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>
        <select className="input" value={courtId} onChange={(e) => setCourtId(e.target.value)}>
          <option value="">Cancha</option>
          {courts.data?.map((c) => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>
        <input className="input" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        <input className="input" type="time" value={time} onChange={(e) => setTime(e.target.value)} />
      </div>
      {matches.data?.items.map((match) => (
        <Link key={match.id} to={`/matches/${match.id}`} className="card block hover:border-accent">
          <p className="font-semibold">{match.title ?? 'Partido'}</p>
          <p className="text-sm text-slate-400">
            {match.clubName} · {match.courtName} · {new Date(match.startTime).toLocaleString()}
          </p>
        </Link>
      ))}
    </div>
  );
}
