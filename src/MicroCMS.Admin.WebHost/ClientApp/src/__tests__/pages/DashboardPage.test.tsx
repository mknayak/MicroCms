import { describe, it, expect } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { render } from '@/test/utils';
import DashboardPage from '@/pages/dashboard/DashboardPage';

describe('DashboardPage', () => {
  it('renders welcome message', async () => {
    render(<DashboardPage />);
    await waitFor(() => {
      expect(screen.getByText(/welcome back/i)).toBeInTheDocument();
    });
  });

  it('displays stats cards', async () => {
    render(<DashboardPage />);
    await waitFor(() => {
      expect(screen.getByText('42')).toBeInTheDocument(); // total entries
      expect(screen.getByText('30')).toBeInTheDocument(); // published
      expect(screen.getByText('12')).toBeInTheDocument(); // drafts
    });
  });

  it('displays stat labels', async () => {
    render(<DashboardPage />);
    await waitFor(() => {
      expect(screen.getByText('Total Entries')).toBeInTheDocument();
      expect(screen.getByText('Published')).toBeInTheDocument();
      expect(screen.getByText('Drafts')).toBeInTheDocument();
    });
  });
});
