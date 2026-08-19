import { Route, Routes } from 'react-router-dom'
import { AppShell } from '@/components/layout/app-shell'
import { AuthGate } from '@/components/layout/auth-gate'
import Dashboard from '@/pages/Dashboard'
import Companies from '@/pages/Companies'
import CompanyDetail from '@/pages/CompanyDetail'
import Facilities from '@/pages/Facilities'
import FacilityDetail from '@/pages/FacilityDetail'
import Search from '@/pages/Search'

export default function App() {
  return (
    <AuthGate>
      <Routes>
        <Route element={<AppShell />}>
          <Route index element={<Dashboard />} />
          <Route path="companies" element={<Companies />} />
          <Route path="companies/:id" element={<CompanyDetail />} />
          <Route path="facilities" element={<Facilities />} />
          <Route path="facilities/:id" element={<FacilityDetail />} />
          <Route path="search" element={<Search />} />
        </Route>
      </Routes>
    </AuthGate>
  )
}
