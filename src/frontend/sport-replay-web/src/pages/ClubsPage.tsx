import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { clubsApi } from '../services/api';
import { useAuth } from '../hooks/useAuth';

export function ClubsPage() {
  const { user } = useAuth();
  const qc = useQueryClient();
  const clubs = useQuery({ queryKey: ['clubs'], queryFn: clubsApi.list });
  const form = useForm({ defaultValues: { name: '', city: '', description: '' } });
  const create = useMutation({
    mutationFn: clubsApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['clubs'] })
  });

  return (
    <div className="space-y-6 pb-16">
      <h1 className="font-display text-3xl">Clubes</h1>
      <div className="grid gap-4 md:grid-cols-2">
        {clubs.data?.map((club) => (
          <Link key={club.id} to={`/clubs/${club.id}`} className="card block hover:border-accent">
            <h2 className="text-xl font-semibold">{club.name}</h2>
            <p className="text-sm text-slate-400">{club.city} · {club.country}</p>
          </Link>
        ))}
      </div>
      {user?.role !== 'Player' && (
        <form className="card space-y-3" onSubmit={form.handleSubmit((v) => create.mutate(v))}>
          <h2 className="font-semibold">Nuevo club</h2>
          <input className="input" placeholder="Nombre" {...form.register('name', { required: true })} />
          <input className="input" placeholder="Ciudad" {...form.register('city')} />
          <textarea className="input" placeholder="Descripcion" {...form.register('description')} />
          <button className="btn-primary">Crear</button>
        </form>
      )}
    </div>
  );
}

export function ClubDetailPage() {
  const { id = '' } = useParams();
  const club = useQuery({ queryKey: ['club', id], queryFn: () => clubsApi.get(id) });
  const courts = useQuery({ queryKey: ['courts', id], queryFn: () => clubsApi.courts(id) });
  const form = useForm({ defaultValues: { name: '', sportType: 1 } });
  const qc = useQueryClient();
  const create = useMutation({
    mutationFn: (payload: { name: string; sportType: number }) => clubsApi.createCourt(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['courts', id] })
  });

  return (
    <div className="space-y-4">
      <h1 className="font-display text-3xl">{club.data?.name}</h1>
      {courts.data?.map((c) => (
        <div key={c.id} className="card">
          <p className="font-semibold">{c.name}</p>
          <p className="text-sm text-slate-400">{c.description}</p>
        </div>
      ))}
      <form className="card space-y-3" onSubmit={form.handleSubmit((v) => create.mutate({ name: v.name, sportType: Number(v.sportType) }))}>
        <h2>Agregar cancha</h2>
        <input className="input" placeholder="Nombre" {...form.register('name', { required: true })} />
        <select className="input" {...form.register('sportType')}>
          <option value="1">Padel</option>
          <option value="2">Tenis</option>
          <option value="3">Futbol</option>
        </select>
        <button className="btn-primary">Crear cancha</button>
      </form>
    </div>
  );
}
