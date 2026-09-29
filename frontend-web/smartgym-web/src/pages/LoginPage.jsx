import React, { useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigate, useLocation } from 'react-router-dom';
import { loginSuccess, setAuthError } from '../store/authSlice';
import authApi from '../services/authApi';
import { Dumbbell, Lock, Mail, AlertCircle, ArrowRight, Shield } from 'lucide-react';

export const LoginPage = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const dispatch = useDispatch();
  const navigate = useNavigate();
  const location = useLocation();

  const from = location.state?.from?.pathname || '/dashboard';

  const handleLogin = async (e) => {
    e?.preventDefault();
    if (!email || !password) {
      setErrorMsg('Please enter both email and password.');
      return;
    }

    setIsLoading(true);
    setErrorMsg('');

    try {
      const data = await authApi.login({ email, password });
      dispatch(loginSuccess(data));
      navigate(from, { replace: true });
    } catch (err) {
      const msg = err.response?.data?.detail || err.response?.data?.message || 'Invalid credentials or login failed.';
      setErrorMsg(msg);
      dispatch(setAuthError(msg));
    } finally {
      setIsLoading(false);
    }
  };

  const handleQuickDemo = (demoEmail, demoPassword) => {
    setEmail(demoEmail);
    setPassword(demoPassword);
    setErrorMsg('');
  };

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'radial-gradient(circle at 50% 20%, rgba(99,102,241,0.15) 0%, rgba(11,15,25,1) 70%)',
      padding: '1.5rem',
    }}>
      <div className="glass-card" style={{
        width: '100%',
        maxWidth: 440,
        padding: '2.5rem',
        borderRadius: 'var(--radius-lg)',
        border: '1px solid rgba(255,255,255,0.1)',
        boxShadow: 'var(--shadow-card)',
      }}>
        {/* Brand Header */}
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <div style={{
            width: 52,
            height: 52,
            borderRadius: 'var(--radius-md)',
            background: 'linear-gradient(135deg, var(--primary), var(--accent-cyan))',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            marginBottom: '1rem',
            boxShadow: '0 8px 20px rgba(99,102,241,0.4)',
          }}>
            <Dumbbell size={28} color="#ffffff" />
          </div>
          <h1 style={{ fontSize: '1.65rem', fontWeight: 800, color: '#ffffff', letterSpacing: '-0.02em' }}>
            SmartGym Enterprise
          </h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.875rem', marginTop: '0.35rem' }}>
            Sign in to access unified operations and analytics
          </p>
        </div>

        {errorMsg && (
          <div style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.65rem',
            padding: '0.75rem 1rem',
            borderRadius: 'var(--radius-sm)',
            background: 'rgba(244,63,94,0.15)',
            border: '1px solid rgba(244,63,94,0.3)',
            color: '#fb7185',
            fontSize: '0.85rem',
            marginBottom: '1.5rem',
          }}>
            <AlertCircle size={18} />
            <span>{errorMsg}</span>
          </div>
        )}

        <form onSubmit={handleLogin} style={{ display: 'flex', flexDirection: 'column', gap: '1.15rem' }}>
          <div className="form-group" style={{ marginBottom: 0 }}>
            <label className="form-label" htmlFor="email-input">
              Email Address
            </label>
            <div style={{ position: 'relative' }}>
              <Mail size={16} color="var(--text-muted)" style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)' }} />
              <input
                id="email-input"
                type="email"
                required
                className="form-input"
                style={{ paddingLeft: '2.4rem' }}
                placeholder="name@smartgym.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
          </div>

          <div className="form-group" style={{ marginBottom: 0 }}>
            <label className="form-label" htmlFor="password-input">
              Password
            </label>
            <div style={{ position: 'relative' }}>
              <Lock size={16} color="var(--text-muted)" style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)' }} />
              <input
                id="password-input"
                type="password"
                required
                className="form-input"
                style={{ paddingLeft: '2.4rem' }}
                placeholder="••••••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>
          </div>

          <button
            type="submit"
            className="btn btn-primary"
            disabled={isLoading}
            style={{ width: '100%', padding: '0.8rem', marginTop: '0.5rem', fontSize: '0.95rem' }}
          >
            {isLoading ? 'Signing In...' : (
              <>
                Sign In to SmartGym <ArrowRight size={16} />
              </>
            )}
          </button>
        </form>

        {/* Quick Demo Credentials */}
        <div style={{
          marginTop: '2rem',
          paddingTop: '1.5rem',
          borderTop: '1px solid var(--border-subtle)',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.85rem' }}>
            <Shield size={14} color="var(--text-muted)" />
            <span style={{ fontSize: '0.75rem', fontWeight: 700, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
              Quick Demo Accounts
            </span>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '0.5rem' }}>
            <button
              type="button"
              className="btn btn-secondary"
              style={{ fontSize: '0.75rem', padding: '0.45rem 0.2rem' }}
              onClick={() => handleQuickDemo('admin@smartgym.com', 'Admin@12345')}
            >
              Admin
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              style={{ fontSize: '0.75rem', padding: '0.45rem 0.2rem' }}
              onClick={() => handleQuickDemo('trainer@smartgym.com', 'Trainer@12345')}
            >
              Trainer
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              style={{ fontSize: '0.75rem', padding: '0.45rem 0.2rem' }}
              onClick={() => handleQuickDemo('member@smartgym.com', 'Member@12345')}
            >
              Member
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
