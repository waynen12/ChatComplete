// src/components/Layout.tsx
import { AppBar, Toolbar, Typography, Container } from '@mui/material';
import { ReactNode } from 'react';

interface LayoutProps {
  children: ReactNode;
}

export default function Layout({ children }: LayoutProps) {
  return (
    <>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" component="div">
            ChatComplete
          </Typography>
        </Toolbar>
      </AppBar>
      <Container sx={{ mt: 4 }}>
        {children}
      </Container>
    </>
  );
}