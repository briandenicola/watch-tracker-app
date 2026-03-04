import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import Markdown from 'react-markdown';
import { getWatch, deleteWatch, deleteWatchImage, analyzeWatch, updateWatch, recordWear } from '../api/watches';
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
  const [detailsOpen, setDetailsOpen] = useState(false);
  const [pendingAnalysis, setPendingAnalysis] = useState<string | null>(null);

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
      setPendingAnalysis(analysis);
    } catch {
      setAnalyzeError('Analysis failed. Make sure the Anthropic API key is configured.');
    } finally {
      setAnalyzing(false);
    }
  }

  async function handleAcceptAnalysis() {
    if (!watch || !pendingAnalysis) return;
    const newNotes = watch.notes
      ? `${watch.notes}\n\n---\n\n${pendingAnalysis}`
      : pendingAnalysis;
    await updateWatch(watch.id, {
      brand: watch.brand,
      model: watch.model,
      movementType: watch.movementType,
      caseSizeMm: watch.caseSizeMm,
      bandType: watch.bandType,
      bandColor: watch.bandColor,
      purchaseDate: watch.purchaseDate,
      purchasePrice: watch.purchasePrice,
      notes: newNotes,
      crystalType: watch.crystalType,
      caseShape: watch.caseShape,
      crownType: watch.crownType,
      calendarType: watch.calendarType,
      countryOfOrigin: watch.countryOfOrigin,
      waterResistance: watch.waterResistance,
      lugWidthMm: watch.lugWidthMm,
      dialColor: watch.dialColor,
      bezelType: watch.bezelType,
      powerReserveHours: watch.powerReserveHours,
      serialNumber: watch.serialNumber,
      batteryType: watch.batteryType,
      linkUrl: watch.linkUrl,
      linkText: watch.linkText,
    });
    setWatch({ ...watch, notes: newNotes });
    setPendingAnalysis(null);
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
        {watch.bandColor && <span className="detail-chip"><strong>Band Color</strong> {watch.bandColor}</span>}
        {watch.purchaseDate && <span className="detail-chip"><strong>Purchased</strong> {fmtDate(watch.purchaseDate)}</span>}
        {watch.purchasePrice != null && <span className="detail-chip"><strong>Price</strong> ${watch.purchasePrice.toFixed(2)}</span>}
        <span className="detail-chip">
          <strong>Worn</strong> {watch.timesWorn} {watch.timesWorn === 1 ? 'time' : 'times'}
        </span>
        {watch.lastWornDate && (
          <span className="detail-chip"><strong>Last Worn</strong> {fmtDate(watch.lastWornDate)}</span>
        )}
        {watch.linkUrl && (
          <a href={watch.linkUrl} target="_blank" rel="noopener noreferrer" className="detail-chip" style={{ textDecoration: 'none' }}>
            <strong>Link</strong> {watch.linkText || watch.linkUrl}
          </a>
        )}
      </div>

      {(() => {
        const extras: { label: string; value: string }[] = [];
        if (watch.crystalType) extras.push({ label: 'Crystal', value: watch.crystalType });
        if (watch.caseShape) extras.push({ label: 'Case Shape', value: watch.caseShape });
        if (watch.crownType) extras.push({ label: 'Crown', value: watch.crownType });
        if (watch.calendarType) extras.push({ label: 'Calendar', value: watch.calendarType });
        if (watch.countryOfOrigin) extras.push({ label: 'Origin', value: watch.countryOfOrigin });
        if (watch.waterResistance) extras.push({ label: 'Water Resistance', value: watch.waterResistance });
        if (watch.lugWidthMm) extras.push({ label: 'Lug Width', value: `${watch.lugWidthMm}mm` });
        if (watch.dialColor) extras.push({ label: 'Dial', value: watch.dialColor });
        if (watch.bezelType) extras.push({ label: 'Bezel', value: watch.bezelType });
        if (watch.serialNumber) extras.push({ label: 'Serial / Ref', value: watch.serialNumber });
        if (watch.batteryType) extras.push({ label: 'Battery', value: watch.batteryType });
        if (extras.length === 0 && !watch.notes) return null;
        return (
          <div className="accordion" style={{ marginBottom: '1rem' }}>
            <button type="button" className="accordion-toggle" onClick={() => setDetailsOpen(!detailsOpen)}>
              Additional Details
              <span className={`accordion-chevron${detailsOpen ? ' open' : ''}`}>▼</span>
            </button>
            <div className={`accordion-content${detailsOpen ? ' open' : ''}`}>
              <div className="accordion-inner">
                {extras.length > 0 && (
                  <div className="watch-detail-chips" style={{ marginTop: '0.75rem' }}>
                    {extras.map((e) => (
                      <span key={e.label} className="detail-chip"><strong>{e.label}</strong> {e.value}</span>
                    ))}
                  </div>
                )}
                {watch.notes && (
                  <>
                    <h3 style={{ marginTop: '0.75rem' }}>Notes</h3>
                    <div className="watch-detail-notes watch-detail-notes-scroll">
                      <Markdown>{watch.notes}</Markdown>
                    </div>
                  </>
                )}
              </div>
            </div>
          </div>
        );
      })()}

      {watch.imageUrls.length > 0 && (
        <div className="watch-images-section">
          <ImageCarousel images={watch.imageUrls} alt={`${watch.brand} ${watch.model}`} />
          <div className="image-actions">
            <button className="btn btn-sm" onClick={handleAnalyze} disabled={analyzing}>
              {analyzing ? 'Analyzing…' : '🤖 Analyze with AI'}
            </button>
            <button className="btn btn-danger btn-sm" onClick={() => handleDeleteImage(watch.imageUrls[0]?.id)}>
              Delete Current Image
            </button>
          </div>
          {analyzeError && <p className="error">{analyzeError}</p>}
        </div>
      )}

      {pendingAnalysis !== null && (
        <div className="modal-overlay" onClick={() => setPendingAnalysis(null)}>
          <div className="modal analysis-modal" onClick={(e) => e.stopPropagation()}>
            <h3>AI Analysis</h3>
            <div className="analysis-modal-body">
              <Markdown>{pendingAnalysis}</Markdown>
            </div>
            <div className="modal-actions">
              <button className="btn" onClick={handleAcceptAnalysis}>Accept &amp; Save to Notes</button>
              <button className="btn btn-danger" onClick={() => setPendingAnalysis(null)}>Dismiss</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
