import { useCallback, useRef, useState, type ReactNode } from 'react';

interface Props {
  onRefresh: () => Promise<void>;
  children: ReactNode;
}

const THRESHOLD = 80;

export default function PullToRefresh({ onRefresh, children }: Props) {
  const [pullDistance, setPullDistance] = useState(0);
  const [refreshing, setRefreshing] = useState(false);
  const startY = useRef(0);
  const pulling = useRef(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    if (refreshing) return;
    // Only activate when scrolled to top
    if (window.scrollY > 0) return;
    startY.current = e.touches[0].clientY;
    pulling.current = true;
  }, [refreshing]);

  const handleTouchMove = useCallback((e: React.TouchEvent) => {
    if (!pulling.current || refreshing) return;
    const dy = e.touches[0].clientY - startY.current;
    if (dy < 0) {
      setPullDistance(0);
      return;
    }
    // Apply resistance — diminishing returns past threshold
    const distance = Math.min(dy * 0.5, THRESHOLD * 1.5);
    setPullDistance(distance);
  }, [refreshing]);

  const handleTouchEnd = useCallback(async () => {
    if (!pulling.current || refreshing) return;
    pulling.current = false;
    if (pullDistance >= THRESHOLD) {
      setRefreshing(true);
      setPullDistance(THRESHOLD);
      try {
        await onRefresh();
      } finally {
        setRefreshing(false);
        setPullDistance(0);
      }
    } else {
      setPullDistance(0);
    }
  }, [pullDistance, refreshing, onRefresh]);

  const progress = Math.min(pullDistance / THRESHOLD, 1);

  return (
    <div
      ref={containerRef}
      className="pull-to-refresh"
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
    >
      <div
        className="pull-to-refresh-indicator"
        style={{
          height: pullDistance > 0 || refreshing ? `${pullDistance}px` : '0',
          opacity: progress,
        }}
      >
        <div
          className={`pull-to-refresh-spinner${refreshing ? ' spinning' : ''}`}
          style={{ transform: `rotate(${progress * 360}deg)` }}
        >
          ↻
        </div>
        <span className="pull-to-refresh-text">
          {refreshing ? 'Refreshing…' : pullDistance >= THRESHOLD ? 'Release to refresh' : 'Pull to refresh'}
        </span>
      </div>
      {children}
    </div>
  );
}
