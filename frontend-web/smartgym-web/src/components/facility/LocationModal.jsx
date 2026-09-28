import React, { useState, useEffect } from 'react';
import { X, Save, MapPin } from 'lucide-react';
import facilityApi from '../../services/facilityApi';

export const LocationModal = ({ location, onClose, onSaved }) => {
  const [formData, setFormData] = useState({
    name: '',
    floor: 'Ground Floor',
    description: '',
  });
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    if (location) {
      setFormData({
        name: location.name || '',
        floor: location.floor || 'Ground Floor',
        description: location.description || '',
      });
    }
  }, [location]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name) {
      setErrorMsg('Location name is required.');
      return;
    }

    try {
      setSubmitting(true);
      setErrorMsg('');

      const payload = {
        name: formData.name.trim(),
        floor: formData.floor.trim(),
        description: formData.description.trim(),
      };

      if (location?.id) {
        await facilityApi.updateLocation(location.id, payload);
      } else {
        await facilityApi.createLocation(payload);
      }

      onSaved();
    } catch (err) {
      setErrorMsg(err.response?.data?.detail || err.message || 'Failed to save location.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.75)', backdropFilter: 'blur(5px)',
      display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000, padding: '1.5rem'
    }}>
      <div className="glass-card" style={{
        maxWidth: '550px', width: '100%', borderRadius: '16px', padding: '2rem', border: '1px solid rgba(255,255,255,0.1)'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <h3 style={{ fontSize: '1.35rem', fontWeight: 800, display: 'flex', alignItems: 'center', gap: '8px' }}>
            <MapPin size={22} color="var(--primary)" />
            {location?.id ? 'Edit Facility Location' : 'Add Facility Location'}
          </h3>
          <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--text-secondary)', cursor: 'pointer' }}>
            <X size={22} />
          </button>
        </div>

        {errorMsg && (
          <div style={{
            backgroundColor: 'rgba(239, 68, 68, 0.15)', border: '1px solid rgba(239, 68, 68, 0.3)',
            color: '#F87171', padding: '0.75rem 1rem', borderRadius: '8px', marginBottom: '1.25rem', fontSize: '0.875rem'
          }}>
            {errorMsg}
          </div>
        )}

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <div>
            <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '4px' }}>
              Location / Area Name *
            </label>
            <input
              required
              type="text"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="e.g. Cardio Zone A, Recovery Suite, Studio 1"
              style={{ width: '100%', padding: '0.65rem 0.85rem', borderRadius: '8px', backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.15)', color: 'white' }}
            />
          </div>

          <div>
            <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '4px' }}>
              Floor Level
            </label>
            <select
              value={formData.floor}
              onChange={(e) => setFormData({ ...formData, floor: e.target.value })}
              style={{ width: '100%', padding: '0.65rem 0.85rem', borderRadius: '8px', backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.15)', color: 'white' }}
            >
              <option value="Basement">Basement</option>
              <option value="Ground Floor">Ground Floor</option>
              <option value="1st Floor">1st Floor</option>
              <option value="2nd Floor">2nd Floor</option>
              <option value="Mezzanine">Mezzanine</option>
              <option value="Rooftop">Rooftop</option>
            </select>
          </div>

          <div>
            <label style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '4px' }}>
              Description
            </label>
            <textarea
              rows={3}
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              placeholder="Facility features, equipment types housed, restrictions..."
              style={{ width: '100%', padding: '0.65rem 0.85rem', borderRadius: '8px', backgroundColor: 'rgba(0,0,0,0.4)', border: '1px solid rgba(255,255,255,0.15)', color: 'white' }}
            />
          </div>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1rem' }}>
            <button type="button" onClick={onClose} className="btn-secondary">
              Cancel
            </button>
            <button type="submit" disabled={submitting} className="btn-primary" style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <Save size={16} /> {submitting ? 'Saving...' : 'Save Location'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default LocationModal;
