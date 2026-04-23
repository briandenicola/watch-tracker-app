import { useState, useEffect } from 'react';
import type { WatchImage } from '../types';
import { imageUrl } from '../api/watches';

interface ImageCarouselProps {
  images: WatchImage[];
  alt: string;
  onIndexChange?: (index: number) => void;
}

export default function ImageCarousel({ images, alt, onIndexChange }: ImageCarouselProps) {
  const [index, setIndex] = useState(0);

  // Clamp index to valid range without calling setState during render or in an effect
  const safeIndex = images.length > 0 ? Math.min(index, images.length - 1) : 0;

  useEffect(() => {
    onIndexChange?.(safeIndex);
  }, [safeIndex, onIndexChange]);

  if (images.length === 0) return null;

  const prev = () => setIndex((i) => (i - 1 + images.length) % images.length);
  const next = () => setIndex((i) => (i + 1) % images.length);

  return (
    <div className="carousel">
      <div className="carousel-viewport">
        <img src={imageUrl(images[safeIndex].url)} alt={`${alt} ${safeIndex + 1}`} className="carousel-image" />
        {images.length > 1 && (
          <>
            <button className="carousel-btn carousel-prev" onClick={prev} aria-label="Previous image">‹</button>
            <button className="carousel-btn carousel-next" onClick={next} aria-label="Next image">›</button>
          </>
        )}
      </div>
      {images.length > 1 && (
        <div className="carousel-dots">
          {images.map((_, i) => (
            <button
              key={i}
              className={`carousel-dot${i === safeIndex ? ' active' : ''}`}
              onClick={() => setIndex(i)}
              aria-label={`Go to image ${i + 1}`}
            />
          ))}
        </div>
      )}
    </div>
  );
}
