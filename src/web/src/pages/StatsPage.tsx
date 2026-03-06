import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { getWearLogs } from '../api/watches';
import { usePreferences } from '../context/PreferencesContext';
import type { WearLog } from '../types';

const CLOUD_COLORS = [
  '#e74c3c', '#3498db', '#2ecc71', '#f39c12', '#9b59b6',
  '#1abc9c', '#e67e22', '#2980b9', '#c0392b', '#16a085',
  '#8e44ad', '#d35400', '#27ae60', '#2c3e50', '#f1c40f',
];

interface CloudWord {
  text: string;
  count: number;
  size: number;
  color: string;
  rotate: number;
}

export default function StatsPage() {
  const [logs, setLogs] = useState<WearLog[]>([]);
  const [loading, setLoading] = useState(true);
  const { timezone } = usePreferences();

  useEffect(() => {
    getWearLogs()
      .then(setLogs)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const words: CloudWord[] = useMemo(() => {
    const counts: Record<string, number> = {};
    for (const log of logs) {
      const key = `${log.watchBrand} ${log.watchModel}`;
      counts[key] = (counts[key] ?? 0) + 1;
    }

    const entries = Object.entries(counts).sort((a, b) => b[1] - a[1]);
    if (entries.length === 0) return [];

    const max = entries[0][1];
    const min = entries[entries.length - 1][1];
    const minSize = 0.85;
    const maxSize = 3.5;

    return entries.map(([text, count], i) => {
      const ratio = max === min ? 1 : (count - min) / (max - min);
      const size = minSize + ratio * (maxSize - minSize);
      const rotate = (i % 5 === 0) ? (Math.random() > 0.5 ? 12 : -12) : 0;
      return {
        text,
        count,
        size,
        color: CLOUD_COLORS[i % CLOUD_COLORS.length],
        rotate,
      };
    });
  }, [logs]);

  // Shuffle words for a natural cloud layout
  const shuffledWords = useMemo(() => {
    const arr = [...words];
    for (let i = arr.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [arr[i], arr[j]] = [arr[j], arr[i]];
    }
    return arr;
  }, [words]);

  const totalWears = logs.length;
  const uniqueWatches = new Set(logs.map((l) => l.watchId)).size;

  const timeline = logs;

  function fmtDate(iso: string) {
    return new Date(iso).toLocaleDateString('en-US', { timeZone: timezone, month: 'short', day: 'numeric', year: 'numeric' });
  }

  if (loading) return <p>Loading…</p>;

  return (
    <div className="stats-page">
      <div className="page-header">
        <h1>Wear Stats</h1>
        <Link to="/" className="btn btn-sm">← Back</Link>
      </div>

      <div className="stats-summary">
        <div className="stats-summary-card">
          <span className="stats-summary-value">{totalWears}</span>
          <span className="stats-summary-label">Total Wears</span>
        </div>
        <div className="stats-summary-card">
          <span className="stats-summary-value">{uniqueWatches}</span>
          <span className="stats-summary-label">Watches Worn</span>
        </div>
      </div>

      <section className="stats-section">
        <h2>Wear Cloud</h2>
        {shuffledWords.length === 0 ? (
          <p className="stats-empty">No wear events recorded yet. Hit "Wore Today" on a watch to get started!</p>
        ) : (
          <div className="word-cloud">
            {shuffledWords.map((w) => (
              <span
                key={w.text}
                className="word-cloud-word"
                style={{
                  fontSize: `${w.size}rem`,
                  color: w.color,
                  transform: w.rotate ? `rotate(${w.rotate}deg)` : undefined,
                }}
                title={`${w.text}: worn ${w.count} time${w.count !== 1 ? 's' : ''}`}
              >
                {w.text}
              </span>
            ))}
          </div>
        )}
      </section>

      <section className="stats-section">
        <h2>Recent Wear Timeline</h2>
        {timeline.length === 0 ? (
          <p className="stats-empty">No wear events recorded yet.</p>
        ) : (
          <div className="wear-timeline">
            {timeline.map((log) => (
              <Link to={`/watches/${log.watchId}`} key={log.id} className="wear-timeline-entry">
                <span className="wear-timeline-dot" />
                <div className="wear-timeline-content">
                  <strong>{log.watchBrand} {log.watchModel}</strong>
                  <span className="wear-timeline-date">{fmtDate(log.wornDate)}</span>
                </div>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
