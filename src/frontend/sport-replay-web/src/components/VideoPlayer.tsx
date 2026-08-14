import { useEffect, useRef } from 'react';
import Hls from 'hls.js';

export function VideoPlayer({ src, kind }: { src?: string; kind?: string }) {
  const ref = useRef<HTMLVideoElement>(null);

  useEffect(() => {
    const video = ref.current;
    if (!video || !src) return;
    if (kind === 'hls' && Hls.isSupported()) {
      const hls = new Hls();
      hls.loadSource(src);
      hls.attachMedia(video);
      return () => hls.destroy();
    }
    video.src = src;
  }, [src, kind]);

  return (
    <video ref={ref} controls className="w-full rounded-2xl bg-black" playsInline>
      Tu navegador no soporta video HTML5.
    </video>
  );
}
