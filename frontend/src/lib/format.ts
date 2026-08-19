import { format, parseISO } from 'date-fns'
import type { Currency } from '@/types/api'

const amountFormatter = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

export function money(amount: number, currency: Currency | string): string {
  return `${currency} ${amountFormatter.format(amount)}`
}

export function moneyAmount(amount: number): string {
  return amountFormatter.format(amount)
}

export function formatDate(isoDate: string): string {
  return format(parseISO(isoDate), 'd MMM yyyy')
}

export function formatDateTime(isoDateTime: string): string {
  const value = isoDateTime.endsWith('Z') ? isoDateTime : `${isoDateTime}Z`
  return format(new Date(value), 'd MMM yyyy, HH:mm')
}

export function percent(rate: number, decimals = 2): string {
  return `${rate.toFixed(decimals)}%`
}

export function toDateInputValue(date: Date): string {
  return format(date, 'yyyy-MM-dd')
}
