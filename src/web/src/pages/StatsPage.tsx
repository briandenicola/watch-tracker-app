import { useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { getWearLogs, deleteWearLog, updateWearLogDate } from '../api/watches';
import { usePreferences } from '../context/PreferencesContext';
import type { WearLog } from '../types';

const CLOUD_VAR_COUNT = 10;

interface CloudWord {
  text: string;
  count: number;
  size: number;
  colorVar: string;
  opacity: number;
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
    const minSize = 0.9;
    const maxSize = 3;

    return entries.map(([text, count], i) => {
      const ratio = max === min ? 1 : (count - min) / (max - min);
      const size = minSize + ratio * (maxSize - minSize);
      const opacity = 0.5 + ratio * 0.5;
      return {
        text,
        count,
        size,
        colorVar: `var(--cloud-${(i % CLOUD_VAR_COUNT) + 1})`,
        opacity,
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

  async function handleDeleteLog(logId: number) {
    if (!confirm('Remove this wear entry?')) return;
    try {
      await deleteWearLog(logId);
      setLogs((prev) => prev.filter((l) => l.id !== logId));
    } catch { /* ignore */ }
  }

  const dateInputRefs = useRef<Record<number, HTMLInputElement | null>>({});

  async function handleDateChange(logId: number, newDate: string) {
    if (!newDate) return;
    try {
      await updateWearLogDate(logId, newDate);
      setLogs((prev) =>
        prev.map((l) => l.id === logId ? { ...l, wornDate: newDate } : l)
          .sort((a, b) => new Date(b.wornDate).getTime() - new Date(a.wornDate).getTime())
      );
    } catch { /* ignore */ }
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
                  color: w.colorVar,
                  opacity: w.opacity,
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
              <div key={log.id} className="wear-timeline-entry">
                <span className="wear-timeline-dot" />
                <Link to={`/watches/${log.watchId}`} className="wear-timeline-content">
                  <strong>{log.watchBrand} {log.watchModel}</strong>
                  <span className="wear-timeline-date">{fmtDate(log.wornDate)}</span>
                </Link>
                <input
                  type="date"
                  ref={(el) => { dateInputRefs.current[log.id] = el; }}
                  className="wear-timeline-date-input"
                  value={log.wornDate.slice(0, 10)}
                  onChange={(e) => handleDateChange(log.id, e.target.value)}
                />
                <button
                  className="wear-timeline-edit"
                  onClick={() => {
                    const input = dateInputRefs.current[log.id];
                    if (!input) return;
                    input.focus();
                    input.showPicker();
                  }}
                  title="Change date"
                >
                  ✎
                </button>
                <button
                  className="wear-timeline-delete"
                  onClick={() => handleDeleteLog(log.id)}
                  title="Remove this wear entry"
                >
                  ✕
                </button>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
