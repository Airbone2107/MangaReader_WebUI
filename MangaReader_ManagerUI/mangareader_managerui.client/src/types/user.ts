import { ApiCollectionResponse } from './api';

export interface UserDto {
  id: string;
  userName: string;
  email: string;
  roles: string[];
}

export interface RoleDto {
  id: string;
  name: string;
}

export interface RoleDetailsDto {
  id: string;
  name: string;
  permissions: string[];
}

export interface CreateUserRequestDto {
  userName: string;
  email: string;
  password: string;
  roles: string[];
}

export interface UpdateUserRolesRequestDto {
  roles: string[];
}

export interface UpdateRolePermissionsRequestDto {
  permissions: string[];
}

export interface PagedResult<T> extends ApiCollectionResponse<T> {
  items: T[];
  total: number;
  offset: number;
  limit: number;
} 