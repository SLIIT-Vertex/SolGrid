import { createBrowserRouter } from 'react-router-dom'
import { ProtectedRoute } from '@/auth/ProtectedRoute'
import { AppLayout } from '@/components/layout/AppLayout'
import { LoginPage } from '@/features/auth/pages/LoginPage'
import { DashboardPage } from '@/features/dashboard/pages/DashboardPage'
import { ForbiddenPage } from '@/features/shared/pages/ForbiddenPage'
import { CreateMicrogridNodePage } from '@/features/microgrid/pages/CreateMicrogridNodePage'
import { EditMicrogridNodePage } from '@/features/microgrid/pages/EditMicrogridNodePage'
import { MicrogridListPage } from '@/features/microgrid/pages/MicrogridListPage'
import { MicrogridNodeDetailsPage } from '@/features/microgrid/pages/MicrogridNodeDetailsPage'
import { ProsumersListPage } from '@/features/prosumers/pages/ProsumersListPage'
import { ReservationsPage } from '@/features/reservations/pages/ReservationsPage'
import { UserEditPage } from '@/features/users/pages/UserEditPage'
import { UsersListPage } from '@/features/users/pages/UsersListPage'

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/403', element: <ForbiddenPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { path: '/', element: <DashboardPage /> },
          {
            element: <ProtectedRoute allowedRoles={['Backoffice', 'GridOperator']} />,
            children: [{ path: '/reservations', element: <ReservationsPage /> }],
          },
          {
            path: 'microgrid',
            children: [
              { index: true, element: <MicrogridListPage /> },
              {
                element: <ProtectedRoute allowedRoles={['Backoffice']} />,
                children: [
                  { path: 'new', element: <CreateMicrogridNodePage /> },
                  { path: ':id/edit', element: <EditMicrogridNodePage /> },
                ],
              },
              { path: ':id', element: <MicrogridNodeDetailsPage /> },
            ],
          },
          {
            element: <ProtectedRoute allowedRoles={['Backoffice']} />,
            children: [
              { path: '/users', element: <UsersListPage /> },
              { path: '/users/:id/edit', element: <UserEditPage /> },
              { path: '/prosumers', element: <ProsumersListPage /> },
            ],
          },
        ],
      },
    ],
  },
])
