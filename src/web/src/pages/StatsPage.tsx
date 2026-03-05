import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { getWearLogs } from '../api/watches';
import { usePreferences } from '../context/PreferencesContext';
import type { WearLog } from '../types';

function toDateKey(iso: string) {
  return iso.slice(0, 10);
}

function getDaysBetween(start: Date, end: Date) {
  const days: string[] = [];
  const d = new Date(start);
  while (d <= end) {
    days.push(d.toISOString().slice(0, 10));
    d.setDate(d.getDate() + 1);
  }
  return days;
}

const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

function getLevel(count: number, max: number): number {
  if (count === 0) return 0;
  if (max <= 1) return 4;
  const ratio = count / max;
  if (ratio <= 0.25) return 1;
  if (ratio <= 0.5) return 2;
  if (ratio <= 0.75) return 3;
  return 4;
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

  const { days, countMap, maxCount, monthMarkers } = useMemo(() => {
    const end = new Date();
    end.setHours(0, 0, 0, 0);
    const start = new Date(end);
    start.setDate(start.getDate() - 364);
    // Align start to Sunday
    start.setDate(start.getDate() - start.getDay());

    const allDays = getDaysBetween(start, end);
    const map: Record<string, number> = {};
    for (const log of logs) {
      const key = toDateKey(log.wornDate);
      map[key] = (map[key] ?? 0) + 1;
    }
    const max = Math.max(0, ...Object.values(map));

    // Month markers for column headers
    const markers: { label: string; col: number }[] = [];
    let lastMonth = -1;
    for (let i = 0; i < allDays.length; i++) {
      const d = new Date(allDays[i] + 'T00:00:00');
      const month = d.getMonth();
      if (month !== lastMonth) {
        // Column = week index
        markers.push({ label: MONTH_LABELS[month], col: Math.floor(i / 7) });
        lastMonth = month;
      }
    }

    return { days: allDays, countMap: map, maxCount: max, monthMarkers: markers };
  }, [logs]);

  // Build week columns for the grid
  const weeks = useMemo(() => {
    const result: string[][] = [];
    for (let i = 0; i < days.length; i += 7) {
      result.push(days.slice(i, i + 7));
    }
    return result;
  }, [days]);

  const totalWears = logs.length;
  const uniqueWatches = new Set(logs.map((l) => l.watchId)).size;

  // Recent timeline (last 30 entries)
  const timeline = useMemo(() => logs.slice(0, 30), [logs]);

  function fmtDate(iso: string) {
    return new Date(iso).toLocaleDateString('en-US', { timeZone: timezone, month: 'short', day: 'numeric', year: 'numeric' });
  }

  if (loading) return <p>Loading…</p>;

  return (
    <div className="stats-page">
      <div className="page-header">
        <h1>📊 Wear Stats</h1>
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
        <h2>Wear Heatmap</h2>
        <div className="heatmap-scroll">
          <div className="heatmap">
            <div className="heatmap-day-labels">
              <span></span>
              <span>Mon</span>
              <span></span>
              <span>Wed</span>
              <span></span>
              <span>Fri</span>
              <span></span>
            </div>
            <div className="heatmap-grid-wrapper">
              <div className="heatmap-month-labels">
                {monthMarkers.map((m, i) => (
                  <span key={i} style={{ gridColumnStart: m.col + 1 }}>{m.label}</span>
                ))}
              </div>
              <div className="heatmap-grid" style={{ gridTemplateColumns: `repeat(${weeks.length}, 14px)` }}>
                {weeks.map((week, wi) =>
                  week.map((day, di) => {
                    const count = countMap[day] ?? 0;
                    const level = getLevel(count, maxCount);
                    const d = new Date(day + 'T00:00:00');
                    const title = `${d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}: ${count} wear${count !== 1 ? 's' : ''}`;
                    return (
                      <div
                        key={`${wi}-${di}`}
                        className={`heatmap-cell level-${level}`}
                        title={title}
                        style={{ gridColumn: wi + 1, gridRow: di + 1 }}
                      />
                    );
                  })
                )}
              </div>
            </div>
          </div>
          <div className="heatmap-legend">
            <span>Less</span>
            <div className="heatmap-cell level-0" />
            <div className="heatmap-cell level-1" />
            <div className="heatmap-cell level-2" />
            <div className="heatmap-cell level-3" />
            <div className="heatmap-cell level-4" />
            <span>More</span>
          </div>
        </div>
      </section>

      <section className="stats-section">
        <h2>Recent Wear Timeline</h2>
        {timeline.length === 0 ? (
          <p className="stats-empty">No wear events recorded yet. Hit "⌚ Wore Today" on a watch to get started!</p>
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
