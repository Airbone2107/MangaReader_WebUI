import { format, parseISO } from 'date-fns'

export const formatDate = (dateString, formatStr = 'dd/MM/yyyy HH:mm:ss') => {
  if (!dateString) return ''
  try {
    const date = parseISO(dateString)
    return format(date, formatStr)
  } catch (error) {
    console.error('Error formatting date:', dateString, error)
    return dateString
  }
} 