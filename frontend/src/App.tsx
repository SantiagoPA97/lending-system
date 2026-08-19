import { lazy, Suspense } from 'react'
import { Route, Routes } from 'react-router-dom'
import { AppShell } from '@/components/layout/app-shell'
import { AuthGate } from '@/components/layout/auth-gate'
import { SkeletonRows } from '@/components/ui/skeleton'

const Dashboard = lazy(() => import('@/pages/Dashboard'))
const Companies = lazy(() => import('@/pages/Companies'))
const CompanyDetail = lazy(() => import('@/pages/CompanyDetail'))
const Facilities = lazy(() => import('@/pages/Facilities'))
const FacilityDetail = lazy(() => import('@/pages/FacilityDetail'))
const Search = lazy(() => import('@/pages/Search'))
const Assistant = lazy(() => import('@/pages/Assistant'))

function PageFallback() {
  return <SkeletonRows rows={8} className="p-0 pt-2" />
}

function page(element: React.ReactNode) {
  return <Suspense fallback={<PageFallback />}>{element}</Suspense>
}

export default function App() {
  return (
    <AuthGate>
      <Routes>
        <Route element={<AppShell />}>
          <Route index element={page(<Dashboard />)} />
          <Route path="companies" element={page(<Companies />)} />
          <Route path="companies/:id" element={page(<CompanyDetail />)} />
          <Route path="facilities" element={page(<Facilities />)} />
          <Route path="facilities/:id" element={page(<FacilityDetail />)} />
          <Route path="search" element={page(<Search />)} />
          <Route path="assistant" element={page(<Assistant />)} />
        </Route>
      </Routes>
    </AuthGate>
  )
}
