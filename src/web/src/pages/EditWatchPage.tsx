import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getWatch, updateWatch, uploadWatchImages, setCoverImage, imageUrl, getWatches } from '../api/watches';
import WatchForm from '../components/WatchForm';
import type { Watch, CreateWatch } from '../types';

export default function EditWatchPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [watch, setWatch] = useState<Watch | null>(null);
  const [loading, setLoading] = useState(true);
  const [brands, setBrands] = useState<string[]>([]);

  useEffect(() => {
    if (!id) return;
    getWatch(Number(id))
      .then(setWatch)
      .catch(() => navigate('/'))
      .finally(() => setLoading(false));
  }, [id, navigate]);

  useEffect(() => {
    getWatches().then((watches) => {
      const unique = [...new Set(watches.map((w) => w.brand))].sort();
      setBrands(unique);
    }).catch(() => {});
  }, []);

  async function handleSubmit(data: CreateWatch, files: File[]) {
    if (!id) return;
    await updateWatch(Number(id), data);
    if (files.length > 0) {
      await uploadWatchImages(Number(id), files);
    }
    navigate(`/watches/${id}`);
  }

  async function handleSetCover(imageId: number) {
    if (!watch) return;
    await setCoverImage(watch.id, imageId);
    const updated = await getWatch(watch.id);
    setWatch(updated);
  }

  if (loading) return <p>Loading…</p>;
  if (!watch) return <p>Watch not found.</p>;

  return (
    <div className="watch-form-page">
      <h1>Edit {watch.brand} {watch.model}</h1>

      {watch.imageUrls.length > 1 && (
        <fieldset className="watch-form-group" style={{ marginBottom: '1.25rem', maxWidth: 700 }}>
          <legend>Gallery Image</legend>
          <p style={{ margin: '0 0 0.5rem', fontSize: '0.9rem', color: 'var(--text-muted)' }}>
            Select which image appears in the gallery view.
          </p>
          <div className="cover-image-grid">
            {watch.imageUrls.map((img, idx) => (
              <button
                key={img.id}
                type="button"
                className={`cover-image-option${idx === 0 ? ' active' : ''}`}
                onClick={() => handleSetCover(img.id)}
              >
                <img src={imageUrl(img.url)} alt={`Image ${idx + 1}`} />
                {idx === 0 && <span className="cover-badge">Cover</span>}
              </button>
            ))}
          </div>
        </fieldset>
      )}

      <WatchForm initial={watch} onSubmit={handleSubmit} submitLabel="Update Watch" brands={brands} />
    </div>
  );
}
