import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import Markdown from 'react-markdown';
import { getWatch, deleteWatch, deleteWatchImage, analyzeWatch, recordWear } from '../api/watches';
import ImageCarousel from '../components/ImageCarousel';
import { usePreferences } from '../context/PreferencesContext';
import type { Watch } from '../types';

export default function WatchDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { timezone } = usePreferences();
  const [watch, setWatch] = useState<Watch | null>(null);
  const [loading, setLoading] = useState(true);
  const [analyzing, setAnalyzing] = useState(false);
  const [analyzeError, setAnalyzeError] = useState('');

  useEffect(() => {
    if (!id) return;
    getWatch(Number(id))
      .then(setWatch)
      .catch(() => navigate('/'))
      .finally(() => setLoading(false));
  }, [id, navigate]);

  async function handleDelete() {
    if (!watch || !confirm('Delete this watch?')) return;
    await deleteWatch(watch.id);
    navigate('/');
  }

  async function handleDeleteImage(imageId: number) {
    if (!watch || !confirm('Delete this image?')) return;
    await deleteWatchImage(watch.id, imageId);
    setWatch({ ...watch, imageUrls: watch.imageUrls.filter((img) => img.id !== imageId) });
  }

  async function handleAnalyze() {
    if (!watch) return;
    setAnalyzing(true);
    setAnalyzeError('');
    try {
      const analysis = await analyzeWatch(watch.id);
      setWatch({ ...watch, aiAnalysis: analysis });
    } catch {
      setAnalyzeError('Analysis failed. Make sure the Anthropic API key is configured.');
    } finally {
      setAnalyzing(false);
    }
  }

  async function handleRecordWear() {
    if (!watch) return;
    const updated = await recordWear(watch.id);
    setWatch(updated);
  }

  function fmtDate(iso: string) {
    return new Date(iso).toLocaleDateString('en-US', { timeZone: timezone });
  }

  if (loading) return <p>Loading…</p>;
  if (!watch) return <p>Watch not found.</p>;

  return (
    <div className="watch-detail">
      <Link to="/" className="back-link">← Back</Link>
      <div className="watch-detail-header">
        <h1>{watch.brand} {watch.model}</h1>
        <div className="watch-detail-header-actions">
          <button className="btn btn-sm" onClick={handleRecordWear}>⌚ Wore Today</button>
          <Link to={`/watches/${watch.id}/edit`} className="btn btn-sm">Edit</Link>
          <button className="btn btn-danger btn-sm" onClick={handleDelete}>Delete</button>
        </div>
      </div>

      <div className="watch-detail-chips">
        <span className="detail-chip"><strong>Movement</strong> {watch.movementType}</span>
        {watch.caseSizeMm && <span className="detail-chip"><strong>Case</strong> {watch.caseSizeMm}mm</span>}
        {watch.bandType && <span className="detail-chip"><strong>Band</strong> {watch.bandType}</span>}
        {watch.purchaseDate && <span className="detail-chip"><strong>Purchased</strong> {fmtDate(watch.purchaseDate)}</span>}
        {watch.purchasePrice != null && <span className="detail-chip"><strong>Price</strong> ${watch.purchasePrice.toFixed(2)}</span>}
        <span className="detail-chip">
          <strong>Worn</strong> {watch.timesWorn} {watch.timesWorn === 1 ? 'time' : 'times'}
        </span>
        {watch.lastWornDate && (
          <span className="detail-chip"><strong>Last Worn</strong> {fmtDate(watch.lastWornDate)}</span>
        )}
      </div>

      {watch.notes && <div className="watch-detail-notes"><Markdown>{watch.notes}</Markdown></div>}

      {watch.imageUrls.length > 0 && (
        <div className="watch-images-section">
          <ImageCarousel images={watch.imageUrls} alt={`${watch.brand} ${watch.model}`} />
          <div className="image-actions">
            <button className="btn btn-sm" onClick={handleAnalyze} disabled={analyzing}>
              {analyzing ? 'Analyzing…' : watch.aiAnalysis ? '🤖 Re-analyze' : '🤖 Analyze with AI'}
            </button>
            <button className="btn btn-danger btn-sm" onClick={() => handleDeleteImage(watch.imageUrls[0]?.id)}>
              Delete Current Image
            </button>
          </div>
          {analyzeError && <p className="error">{analyzeError}</p>}
          {watch.aiAnalysis && (
            <div className="ai-analysis-card">
              <p>{watch.aiAnalysis}</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
