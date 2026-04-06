import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react';

interface Props {
  children: ReactNode[];
  className?: string;
  initialIndex?: number;
  onIndexChange?: (index: number) => void;
}

export default function SwipeGallery({ children, className, initialIndex = 0, onIndexChange }: Props) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [activeIndex, setActiveIndex] = useState(initialIndex);
  const total = children.length;
  const didInitScroll = useRef(false);

  const updateActiveIndex = useCallback(() => {
    const el = scrollRef.current;
    if (!el || total === 0) return;
    const cardWidth = el.scrollWidth / total;
    const idx = Math.round(el.scrollLeft / cardWidth);
    const clamped = Math.max(0, Math.min(idx, total - 1));
    setActiveIndex(clamped);
    onIndexChange?.(clamped);
  }, [total, onIndexChange]);

  // Scroll to initial index on mount
  useEffect(() => {
    const el = scrollRef.current;
    if (!el || total === 0 || didInitScroll.current) return;
    if (initialIndex > 0 && initialIndex < total) {
      const cardWidth = el.scrollWidth / total;
      el.scrollTo({ left: cardWidth * initialIndex, behavior: 'instant' });
    }
    didInitScroll.current = true;
  }, [initialIndex, total]);

  useEffect(() => {
    const el = scrollRef.current;
    if (!el) return;
    el.addEventListener('scroll', updateActiveIndex, { passive: true });
    return () => el.removeEventListener('scroll', updateActiveIndex);
  }, [updateActiveIndex]);

  const scrollTo = (index: number) => {
    const el = scrollRef.current;
    if (!el || total === 0) return;
    const cardWidth = el.scrollWidth / total;
    el.scrollTo({ left: cardWidth * index, behavior: 'smooth' });
  };

  const prev = () => scrollTo(Math.max(0, activeIndex - 1));
  const next = () => scrollTo(Math.min(total - 1, activeIndex + 1));

  if (total === 0) return null;

  return (
    <div className={`swipe-gallery ${className ?? ''}`}>
      <div className="swipe-gallery-track" ref={scrollRef}>
        {children.map((child, i) => (
          <div className="swipe-gallery-slide" key={i}>
            {child}
          </div>
        ))}
      </div>

      <div className="swipe-gallery-controls">
        <button
          className="swipe-gallery-arrow"
          onClick={prev}
          disabled={activeIndex === 0}
          aria-label="Previous"
        >
          ‹
        </button>
        <span className="swipe-gallery-counter">
          {activeIndex + 1} / {total}
        </span>
        <button
          className="swipe-gallery-arrow"
          onClick={next}
          disabled={activeIndex === total - 1}
          aria-label="Next"
        >
          ›
        </button>
      </div>
    </div>
  );
}
