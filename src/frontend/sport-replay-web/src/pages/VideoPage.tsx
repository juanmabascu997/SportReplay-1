import { useMutation, useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { useState } from 'react';
import { paymentsApi, videosApi } from '../services/api';
import { VideoPlayer } from '../components/VideoPlayer';

export function VideoPage() {
  const { id = '' } = useParams();
  const clip = useQuery({ queryKey: ['clip', id], queryFn: () => videosApi.getClip(id) });
  const stream = useQuery({ queryKey: ['stream', id], queryFn: () => videosApi.stream(id), enabled: (clip.data?.status ?? 0) === 2 });
  const [phone, setPhone] = useState('');
  const download = useMutation({ mutationFn: () => videosApi.download(id) });
  const pay = useMutation({
    mutationFn: async () => {
      const request = await videosApi.requestWhatsApp({ matchId: clip.data!.matchId, videoClipId: id, phoneNumber: phone || '+5491100000002' });
      return paymentsApi.create({ videoClipId: id, videoRequestId: request.id, productType: 1 });
    }
  });

  return (
    <div className="space-y-4 pb-16">
      <h1 className="font-display text-3xl">Clip</h1>
      <VideoPlayer src={stream.data?.url ?? clip.data?.streamUrl} kind={stream.data?.kind} />
      <div className="grid gap-3 md:grid-cols-3">
        <button className="btn-primary" onClick={async () => {
          const result = await download.mutateAsync();
          window.open(result.url, '_blank');
        }}>Descargar</button>
        <button className="btn-ghost" onClick={() => navigator.clipboard.writeText(window.location.href)}>Compartir</button>
        <button className="btn-ghost" onClick={() => pay.mutate()}>Enviar por WhatsApp</button>
      </div>
      <input className="input" placeholder="Telefono WhatsApp" value={phone} onChange={(e) => setPhone(e.target.value)} />
      {pay.data?.initPoint && (
        <a className="btn-primary inline-flex" href={pay.data.initPoint}>Pagar con Mercado Pago</a>
      )}
    </div>
  );
}
