export interface CreateCategoryRequest {
  name: string
}

export interface UpdateCategoryRequest {
  name: string
  isActive: boolean
}

export interface CreatePriorityRequest {
  name: string
  sortOrder: number
}

export interface UpdatePriorityRequest {
  name: string
  sortOrder: number
  isActive: boolean
}

export interface CreateStatusRequest {
  name: string
  sortOrder: number
  isClosed: boolean
}

export interface UpdateStatusRequest {
  name: string
  sortOrder: number
  isClosed: boolean
  isActive: boolean
}

export interface CreateUserRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  companyId: string
  role: string
}
