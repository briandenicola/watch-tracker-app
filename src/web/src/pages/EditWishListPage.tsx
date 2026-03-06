import { useEffect, useState, type FormEvent } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getWatch, updateWatch, deleteWatch, importImageFromUrl, deleteWatchImage, imageUrl } from '../api/watches';
import type { Watch } from '../types';

export default function EditWishListPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [watch, setWatch] = useState<Watch | null>(null);
  const [loading, setLoading] = useState(true);

  const [brand, setBrand] = useState('');
  const [model, setModel] = useState('');
  const [price, setPrice] = useState('');
  const [linkUrl, setLinkUrl] = useState('');
  const [imageUrlInput, setImageUrlInput] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!id) return;
    getWatch(Number(id))
      .then((w) => {
        setWatch(w);
        setBrand(w.brand);
        setModel(w.model);
        setPrice(w.purchasePrice?.toString() ?? '');
        setLinkUrl(w.linkUrl ?? '');
      })
      .catch(() => navigate('/?wishlist'))
      .finally(() => setLoading(false));
  }, [id, navigate]);

  async function handleSave(e: FormEvent) {
    e.preventDefault();
    if (!id || !watch) return;
    setSaving(true);
    try {
      await updateWatch(Number(id), {
        brand,
        model,
        movementType: watch.movementType ?? 'Automatic',
        isWishList: true,
        ...(price && { purchasePrice: Number(price) }),
        ...(linkUrl && { linkUrl }),
      });
      if (imageUrlInput) {
        // Remove existing images before importing new one
        for (const img of watch.imageUrls) {
          try { await deleteWatchImage(watch.id, img.id); } catch { /* ignore */ }
        }
        try { await importImageFromUrl(watch.id, imageUrlInput); } catch { /* ignore */ }
      }
      navigate('/?wishlist');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!watch || !confirm('Remove this item from your wish list?')) return;
    await deleteWatch(watch.id);
    navigate('/?wishlist');
  }

  function handlePurchased() {
    if (!watch) return;
    deleteWatch(watch.id).then(() => {
      navigate(`/watches/new?brand=${encodeURIComponent(watch.brand)}&model=${encodeURIComponent(watch.model)}`);
    });
  }

  if (loading) return <p>Loading…</p>;
  if (!watch) return <p>Wish list item not found.</p>;

  return (
    <div className="watch-form-page">
      <h1>Edit Wish List Item</h1>

      {watch.imageUrls.length > 0 && (
        <div className="wishlist-image-preview">
          <img src={imageUrl(watch.imageUrls[0].url)} alt={`${watch.brand} ${watch.model}`} />
        </div>
      )}

      <form className="watch-form" onSubmit={handleSave}>
        <fieldset className="watch-form-group">
          <legend>Wish List Watch</legend>
          <div className="watch-form-row">
            <label>
              Brand *
              <input value={brand} onChange={(e) => setBrand(e.target.value)} required />
            </label>
            <label>
              Model *
              <input value={model} onChange={(e) => setModel(e.target.value)} required />
            </label>
          </div>
          <div className="watch-form-row">
            <label>
              Estimated Price
              <input type="number" step="0.01" value={price} onChange={(e) => setPrice(e.target.value)} placeholder="0.00" />
            </label>
            <label>
              Product Page URL
              <input type="url" value={linkUrl} onChange={(e) => setLinkUrl(e.target.value)} placeholder="https://…" />
            </label>
          </div>
          <label>
            {watch.imageUrls.length > 0 ? 'Replace Image URL' : 'Image URL'}
            <input type="url" value={imageUrlInput} onChange={(e) => setImageUrlInput(e.target.value)} placeholder="https://…/image.jpg" />
          </label>
        </fieldset>

        <div className="watch-form-actions">
          <button type="submit" disabled={saving}>{saving ? 'Saving…' : 'Save'}</button>
          <button type="button" className="btn btn-purchased" onClick={handlePurchased}>
            Purchased!
          </button>
          <button type="button" className="btn btn-danger" onClick={handleDelete}>Delete</button>
        </div>
      </form>
    </div>
  );
}
