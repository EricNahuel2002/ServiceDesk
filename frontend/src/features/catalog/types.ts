export interface CategoryDto {
  id: string
  name: string
  isActive: boolean
}

export interface PriorityDto {
  id: string
  name: string
  sortOrder: number
  isActive: boolean
}

export interface StatusDto {
  id: string
  name: string
  sortOrder: number
  isClosed: boolean
  isActive: boolean
}
