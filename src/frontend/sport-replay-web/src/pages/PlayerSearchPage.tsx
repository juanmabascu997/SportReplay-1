import { useQueries, useQuery } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { clubsApi, matchesApi } from '../services/api';
import { ARGENTINA_PROVINCES } from '../utils/roles';
import { matchHasRecording } from '../utils/recordings';

export function PlayerSearchPage() {
  const clubs = useQuery({ queryKey: ['clubs'], queryFn: clubsApi.list });
  const [province, setProvince] = useState('');
  const [clubId, setClubId] = useState('');
  const [courtId, setCourtId] = useState('');
  const [date, setDate] = useState('');

  const clubsInProvince = useMemo(
    () => (clubs.data ?? []).filter((c) => !province || (c.province ?? '') === province),
    [clubs.data, province]
  );
  const courts = useQuery({ queryKey: ['courts', clubId], queryFn: () => clubsApi.courts(clubId), enabled: !!clubId });
  const canSearch = !!province && !!clubId && !!courtId;
  const matches = useQuery({
    queryKey: ['player-matches', province, clubId, courtId, date],
    queryFn: () =>
      matchesApi.search({
        province,
        clubId,
        courtId,
        date: date || undefined
      }),
    enabled: canSearch
  });

  const items = matches.data?.items ?? [];
  const recordingQueries = useQueries({
    queries: items.map((match) => ({
      queryKey: ['recordings', match.id],
      queryFn: () => matchesApi.recordings(match.id)
    }))
  });
  const recordingsReady = recordingQueries.length === 0 || recordingQueries.every((q) => q.isFetched);
  const withRecording = items.filter((match, index) => matchHasRecording(match, recordingQueries[index]?.data));
  const searched = canSearch && (matches.isFetched || matches.isError);

  return (
    <div className="mx-auto max-w-xl space-y-5 pb-16">
      <div>
        <p className="text-sm text-accent">Jugador</p>
        <h1 className="font-display text-3xl">Busca tu partido</h1>
        <p className="mt-1 text-sm text-slate-400">Elegi provincia, club, cancha y partido para ver si hay grabacion.</p>
      </div>

      <div className="card space-y-3">
        <label className="block text-sm text-slate-300">
          Provincia
          <select
            className="input mt-1"
            value={province}
            onChange={(e) => {
              setProvince(e.target.value);
              setClubId('');
              setCourtId('');
            }}
          >
            <option value="">Seleccionar</option>
            {ARGENTINA_PROVINCES.map((p) => (
              <option key={p} value={p}>{p}</option>
            ))}
          </select>
        </label>

        <label className="block text-sm text-slate-300">
          Club
          <select
            className="input mt-1"
            value={clubId}
            disabled={!province}
            onChange={(e) => {
              setClubId(e.target.value);
              setCourtId('');
            }}
          >
            <option value="">Seleccionar</option>
            {clubsInProvince.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        </label>

        <label className="block text-sm text-slate-300">
          Cancha
          <select className="input mt-1" value={courtId} disabled={!clubId} onChange={(e) => setCourtId(e.target.value)}>
            <option value="">Seleccionar</option>
            {courts.data?.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        </label>

        <label className="block text-sm text-slate-300">
          Fecha (opcional)
          <input className="input mt-1" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </label>
      </div>

      {province && clubsInProvince.length === 0 && !clubs.isLoading && (
        <div className="card text-slate-300">No hay clubes cargados en esta provincia.</div>
      )}

      {searched && items.length === 0 && (
        <div className="card space-y-1">
          <p className="font-semibold">No encontramos partidos</p>
          <p className="text-sm text-slate-400">Proba otra fecha o cancha.</p>
        </div>
      )}

      {searched && recordingsReady && items.length > 0 && withRecording.length === 0 && (
        <div className="card space-y-1">
          <p className="font-semibold">No hay grabaciones para esta busqueda</p>
          <p className="text-sm text-slate-400">Hay {items.length} partido(s), pero ninguno tiene grabacion.</p>
        </div>
      )}

      {items.map((match, index) => {
        const available = matchHasRecording(match, recordingQueries[index]?.data);
        return (
          <div key={match.id} className="card space-y-3">
            <div>
              <p className="font-semibold">{match.title ?? 'Partido'}</p>
              <p className="text-sm text-slate-400">
                {match.clubName} · {match.courtName} · {new Date(match.startTime).toLocaleString()}
              </p>
            </div>
            {available ? (
              <div className="flex items-center justify-between gap-3">
                <p className="text-sm text-accent">Grabacion disponible</p>
                <Link className="btn-primary" to={`/matches/${match.id}`}>Crear clip</Link>
              </div>
            ) : (
              <p className="text-sm text-slate-400">
                {recordingQueries[index]?.isFetching ? 'Buscando grabacion...' : 'Sin grabacion para este partido.'}
              </p>
            )}
          </div>
        );
      })}
    </div>
  );
}
