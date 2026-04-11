import { useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { getWearLogs, deleteWearLog, updateWearLogDate, getWatches } from '../api/watches';
import { usePreferences } from '../context/PreferencesContext';
import useIsPwa from '../hooks/useIsPwa';
import type { WearLog, Watch } from '../types';

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
  const [watches, setWatches] = useState<Watch[]>([]);
  const [loading, setLoading] = useState(true);
  const { timezone } = usePreferences();
  const isPwa = useIsPwa();

  useEffect(() => {
    Promise.all([
      getWearLogs().catch(() => [] as WearLog[]),
      getWatches().catch(() => [] as Watch[]),
    ]).then(([wearLogs, allWatches]) => {
      setLogs(wearLogs);
      setWatches(allWatches);
    }).finally(() => setLoading(false));
  }, []);

  const topExpensive = useMemo(() => {
    return watches
      .filter((w) => !w.isWishList && w.purchasePrice != null && w.purchasePrice > 0)
      .sort((a, b) => (b.purchasePrice ?? 0) - (a.purchasePrice ?? 0))
      .slice(0, 5);
  }, [watches]);

  const { longestUnworn, neverWorn } = useMemo(() => {
    const now = new Date();
    const collection = watches.filter((w) => !w.isWishList);
    const worn = collection
      .filter((w) => w.lastWornDate)
      .map((w) => ({
        watch: w,
        daysSince: Math.floor((now.getTime() - new Date(w.lastWornDate!).getTime()) / 86_400_000),
      }))
      .sort((a, b) => b.daysSince - a.daysSince)
      .slice(0, 5);
    const never = collection.filter((w) => !w.lastWornDate);
    return { longestUnworn: worn, neverWorn: never };
  }, [watches]);

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

  function resetDateInput(logId: number) {
    const input = dateInputRefs.current[logId];
    if (input) {
      input.style.position = '';
      input.style.opacity = '';
      input.style.pointerEvents = '';
      input.style.width = '';
      input.style.height = '';
      input.style.clip = '';
    }
  }

  async function handleDateChange(logId: number, newDate: string) {
    resetDateInput(logId);
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
        {!isPwa && <Link to="/" className="btn btn-sm">← Back</Link>}
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

      {topExpensive.length > 0 && (
        <section className="stats-section">
          <h2>Most Valuable</h2>
          <div className="top-expensive-list">
            {topExpensive.map((w, i) => (
              <Link key={w.id} to={`/watches/${w.id}`} className="top-expensive-item">
                <span className="top-expensive-rank">#{i + 1}</span>
                <span className="top-expensive-name">{w.brand} {w.model}</span>
                <span className="top-expensive-price">${w.purchasePrice!.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
              </Link>
            ))}
          </div>
        </section>
      )}

      {(longestUnworn.length > 0 || neverWorn.length > 0) && (
        <section className="stats-section">
          <h2>Neglected Watches</h2>
          <div className="neglected-split">
            {longestUnworn.length > 0 && (
              <div className="neglected-column">
                <h3>Longest Since Worn</h3>
                <div className="top-expensive-list">
                  {longestUnworn.map((item, i) => (
                    <Link key={item.watch.id} to={`/watches/${item.watch.id}`} className="top-expensive-item">
                      <span className="top-expensive-rank">#{i + 1}</span>
                      <span className="top-expensive-name">{item.watch.brand} {item.watch.model}</span>
                      <span className="neglected-days">{item.daysSince}d ago</span>
                    </Link>
                  ))}
                </div>
              </div>
            )}
            {neverWorn.length > 0 && (
              <div className="neglected-column">
                <h3>Never Worn</h3>
                <div className="top-expensive-list">
                  {neverWorn.map((w) => (
                    <Link key={w.id} to={`/watches/${w.id}`} className="top-expensive-item">
                      <span className="top-expensive-name">{w.brand} {w.model}</span>
                    </Link>
                  ))}
                </div>
              </div>
            )}
          </div>
        </section>
      )}

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
                  onBlur={() => resetDateInput(log.id)}
                />
                <button
                  className="wear-timeline-edit"
                  onClick={() => {
                    const input = dateInputRefs.current[log.id];
                    if (!input) return;
                    input.style.position = 'absolute';
                    input.style.opacity = '0';
                    input.style.pointerEvents = 'auto';
                    input.style.width = 'auto';
                    input.style.height = 'auto';
                    input.style.clip = 'auto';
                    input.focus();
                    input.click();
                    try { input.showPicker(); } catch { /* not supported */ }
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
