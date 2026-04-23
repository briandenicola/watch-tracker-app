import { useState, useEffect, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { createWatch, getWatches, importImageFromUrl } from '../api/watches';
import { useToast } from '../components/Toast';

export default function AddWishListPage() {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const [brand, setBrand] = useState('');
  const [model, setModel] = useState('');
  const [price, setPrice] = useState('');
  const [linkUrl, setLinkUrl] = useState('');
  const [imageUrlInput, setImageUrlInput] = useState('');
  const [brands, setBrands] = useState<string[]>([]);
  const [brandFocused, setBrandFocused] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    getWatches().then((watches) => {
      const unique = [...new Set(watches.map((w) => w.brand))].sort();
      setBrands(unique);
    }).catch(() => showToast('Failed to load brands'));
  }, [showToast]);

  useEffect(() => {
    function handleClick() { setBrandFocused(false); }
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  const filteredBrands = brand.length > 0
    ? brands.filter((b) => b.toLowerCase().includes(brand.toLowerCase()) && b.toLowerCase() !== brand.toLowerCase())
    : [];

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      const watch = await createWatch({
        brand,
        model,
        movementType: 'Automatic',
        isWishList: true,
        ...(price && { purchasePrice: Number(price) }),
        ...(linkUrl && { linkUrl }),
      });
      if (imageUrlInput) {
        try { await importImageFromUrl(watch.id, imageUrlInput); } catch { /* ignore image errors */ }
      }
      navigate('/?wishlist');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="watch-form-page">
      <h1>Add to Wish List</h1>
      <form className="watch-form" onSubmit={handleSubmit}>
        <fieldset className="watch-form-group">
          <legend>Wish List Watch</legend>
          <div className="watch-form-row">
            <label>
              Brand *
              <div className="autocomplete" onMouseDown={(e) => e.stopPropagation()}>
                <input
                  value={brand}
                  onChange={(e) => setBrand(e.target.value)}
                  onFocus={() => setBrandFocused(true)}
                  required
                  autoComplete="off"
                />
                {brandFocused && filteredBrands.length > 0 && (
                  <ul className="autocomplete-list">
                    {filteredBrands.map((b) => (
                      <li key={b}>
                        <button type="button" onMouseDown={() => { setBrand(b); setBrandFocused(false); }}>
                          {b}
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
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
            Image URL
            <input type="url" value={imageUrlInput} onChange={(e) => setImageUrlInput(e.target.value)} placeholder="https://…/image.jpg" />
          </label>
        </fieldset>

        <div className="watch-form-actions">
          <button type="submit" disabled={submitting}>{submitting ? 'Saving…' : 'Add to Wish List'}</button>
          <button type="button" className="btn btn-danger" onClick={() => navigate('/')}>Cancel</button>
        </div>
      </form>
    </div>
  );
}
