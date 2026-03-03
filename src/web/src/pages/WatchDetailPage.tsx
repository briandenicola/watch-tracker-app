import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { getWatch, deleteWatch, deleteWatchImage, analyzeWatch } from '../api/watches';
import ImageCarousel from '../components/ImageCarousel';
import type { Watch } from '../types';

export default function WatchDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
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

  if (loading) return <p>Loading…</p>;
  if (!watch) return <p>Watch not found.</p>;

  return (
    <div className="watch-detail">
      <Link to="/" className="back-link">← Back</Link>
      <h1>{watch.brand} {watch.model}</h1>
      {watch.imageUrls.length > 0 && (
        <div className="watch-images-section">
          <ImageCarousel images={watch.imageUrls} alt={`${watch.brand} ${watch.model}`} />
          <div className="image-actions">
            <button className="btn btn-danger btn-sm" onClick={() => handleDeleteImage(watch.imageUrls[0]?.id)}>
              Delete Current Image
            </button>
          </div>
        </div>
      )}
      {watch.imageUrls.length > 0 && (
        <div className="ai-analysis-section">
          <div className="ai-analysis-header">
            <h3>🤖 AI Analysis</h3>
            <button className="btn btn-sm" onClick={handleAnalyze} disabled={analyzing}>
              {analyzing ? 'Analyzing…' : watch.aiAnalysis ? 'Re-analyze' : 'Analyze with AI'}
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
      <dl>
        <dt>Movement</dt><dd>{watch.movementType}</dd>
        {watch.caseSizeMm && <><dt>Case Size</dt><dd>{watch.caseSizeMm}mm</dd></>}
        {watch.bandType && <><dt>Band Type</dt><dd>{watch.bandType}</dd></>}
        {watch.purchaseDate && <><dt>Purchase Date</dt><dd>{new Date(watch.purchaseDate).toLocaleDateString()}</dd></>}
        {watch.purchasePrice != null && <><dt>Purchase Price</dt><dd>${watch.purchasePrice.toFixed(2)}</dd></>}
        {watch.notes && <><dt>Notes</dt><dd>{watch.notes}</dd></>}
      </dl>
      <div className="detail-actions">
        <Link to={`/watches/${watch.id}/edit`} className="btn">Edit</Link>
        <button className="btn btn-danger" onClick={handleDelete}>Delete</button>
      </div>
    </div>
  );
}
