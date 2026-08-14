import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { camerasApi, clubsApi } from '../services/api';

const protocolLabel = ['', 'RTSP', 'ONVIF', 'Simulada'];
const statusLabel = ['OFFLINE', 'ONLINE', 'ERROR'];
const statusClass = ['status-offline', 'status-online', 'status-error'];

export function CamerasPage() {
  const qc = useQueryClient();
  const cameras = useQuery({ queryKey: ['cameras'], queryFn: camerasApi.list });
  const clubs = useQuery({ queryKey: ['clubs'], queryFn: clubsApi.list });
  const clubId = clubs.data?.[0]?.id;
  const courts = useQuery({ queryKey: ['courts', clubId], queryFn: () => clubsApi.courts(clubId!), enabled: !!clubId });
  const form = useForm({
    defaultValues: { name: 'Cam nueva', courtId: '', ipAddress: '10.0.0.20', isSimulated: true }
  });
  const create = useMutation({
    mutationFn: (values: { name: string; courtId: string; ipAddress: string; isSimulated: boolean }) =>
      camerasApi.create(values.courtId, {
        name: values.name,
        ipAddress: values.ipAddress,
        protocol: values.isSimulated ? 3 : 1,
        isSimulated: values.isSimulated,
        resolution: '1280x720',
        fps: 25,
        bitrate: 2000,
        segmentDurationSeconds: 6
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['cameras'] })
  });
    mutationFn: camerasApi.test,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['cameras'] })
  });

  return (
    <div className="space-y-4 pb-16">
      <h1 className="font-display text-3xl">Camaras</h1>
      <div className="overflow-x-auto card">
        <table className="w-full text-left text-sm">
          <thead className="text-slate-400">
            <tr>
              <th className="pb-2">Nombre</th>
              <th>Cancha</th>
              <th>IP</th>
              <th>Protocolo</th>
              <th>Estado</th>
              <th>Heartbeat</th>
              <th>FPS</th>
              <th>Res</th>
              <th>Bitrate</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {cameras.data?.map((cam) => (
              <tr key={cam.id} className="border-t border-line">
                <td className="py-2">{cam.name}</td>
                <td>{cam.courtName}</td>
                <td>{cam.ipAddress}</td>
                <td>{protocolLabel[cam.protocol]}</td>
                <td>
                  <span className={`rounded-full px-2 py-1 text-xs ${statusClass[cam.status]}`}>{statusLabel[cam.status]}</span>
                </td>
                <td>{cam.lastHeartbeat ? new Date(cam.lastHeartbeat).toLocaleTimeString() : '-'}</td>
                <td>{cam.fps}</td>
                <td>{cam.resolution}</td>
                <td>{cam.bitrate}</td>
                <td>
                  <button className="btn-ghost" onClick={() => test.mutate(cam.id)}>
                    Probar conexion
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <form className="card space-y-3" onSubmit={form.handleSubmit((v) => create.mutate(v))}>
        <h2 className="font-semibold">Registrar camara</h2>
        <select className="input" {...form.register('courtId', { required: true })}>
          <option value="">Cancha</option>
          {courts.data?.map((c) => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>
        <input className="input" placeholder="Nombre" {...form.register('name', { required: true })} />
        <input className="input" placeholder="IP" {...form.register('ipAddress')} />
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" {...form.register('isSimulated')} /> Simulada (sin conexion real)
        </label>
        <button className="btn-primary">Guardar</button>
      </form>
    </div>
  );
}
