import http from 'k6/http';
import { check, sleep } from 'k6';

// SmartGym Load & Performance Test Scenario
// Tests concurrent requests, response times, success/failure rates
export const options = {
  stages: [
    { duration: '10s', target: 20 },  // Ramp-up to 20 concurrent users
    { duration: '20s', target: 50 },  // Sustained load at 50 concurrent users
    { duration: '10s', target: 0 },   // Ramp-down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    http_req_failed: ['rate<0.01'],   // Less than 1% failure rate
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const AI_URL = __ENV.AI_URL || 'http://localhost:8000';

export default function () {
  // 1. Health Endpoint Performance Check
  const healthRes = http.get(`${BASE_URL}/health`);
  check(healthRes, {
    'health status is 200': (r) => r.status === 200,
    'health latency < 100ms': (r) => r.timings.duration < 100,
  });

  // 2. Member Authentication Performance Check
  const loginPayload = JSON.stringify({
    email: 'admin@smartgym.com',
    password: 'Admin123!',
  });
  const loginHeaders = { 'Content-Type': 'application/json' };
  const loginRes = http.post(`${BASE_URL}/api/auth/login`, loginPayload, { headers: loginHeaders });
  
  check(loginRes, {
    'login status is 200': (r) => r.status === 200,
    'token received': (r) => JSON.parse(r.body).accessToken !== undefined,
  });

  let token = '';
  if (loginRes.status === 200) {
    token = JSON.parse(loginRes.body).accessToken;
  }

  // 3. Authenticated Facility Issues Query Performance Check
  if (token) {
    const authHeaders = {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    };

    const issuesRes = http.get(`${BASE_URL}/api/facility-issues?pageNumber=1&pageSize=10`, { headers: authHeaders });
    check(issuesRes, {
      'issues query status is 200': (r) => r.status === 200,
      'issues latency < 300ms': (r) => r.timings.duration < 300,
    });

    // 4. AI Workflows Summary Performance Check
    const approvalsRes = http.get(`${BASE_URL}/api/approvals?status=Pending&pageNumber=1&pageSize=10`, { headers: authHeaders });
    check(approvalsRes, {
      'approvals query status is 200': (r) => r.status === 200,
    });
  }

  // 5. AI Microservice Health & Latency
  const aiHealthRes = http.get(`${AI_URL}/health`);
  check(aiHealthRes, {
    'ai health is 200': (r) => r.status === 200,
    'ai latency < 150ms': (r) => r.timings.duration < 150,
  });

  sleep(0.5);
}
