import Layout from '../components/Layout';
import { Box, Button, Stack, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';

export default function LandingPage() {
  const navigate = useNavigate();

  return (
    <Layout>
      <Stack spacing={4} alignItems="center" justifyContent="center" sx={{ minHeight: '60vh' }}>
        <Typography variant="h4">Welcome to ChatComplete</Typography>
        <Stack direction="row" spacing={3}>
          <Button variant="contained" size="large" onClick={() => navigate('/knowledge')}>
            Manage Knowledge
          </Button>
          <Button variant="outlined" size="large" onClick={() => navigate('/chat')}>
            Chat with AI
          </Button>
        </Stack>
      </Stack>
    </Layout>
  );
}
