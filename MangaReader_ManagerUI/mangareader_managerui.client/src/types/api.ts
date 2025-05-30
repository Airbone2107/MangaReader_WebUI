// Các cấu trúc API response chung
export interface ApiErrorDetail {
  id?: string;
  status: number;
  title: string;
  detail?: string;
  context?: Record<string, unknown>;
}

export interface ApiErrorResponse {
  result: 'error';
  errors: ApiErrorDetail[];
}

export interface RelationshipObject {
  id: string;
  type: string; // 'author', 'artist', 'tag', 'cover_art', etc.
}

export interface ResourceObject<TAttributes> {
  id: string;
  type: string; // 'manga', 'author', etc.
  attributes: TAttributes;
  relationships?: RelationshipObject[];
}

export interface ApiResponse<TData> {
  result: 'ok';
  response: 'entity';
  data: TData;
}

export interface ApiCollectionResponse<TData> {
  result: 'ok';
  response: 'collection';
  data: TData[];
  limit: number;
  offset: number;
  total: number;
}

// Đối tượng response khi upload ảnh bìa hoặc chapter page (chỉ trả về publicId)
export interface UploadResponseDto {
  publicId: string;
}

// Đối tượng response khi tạo chapter page entry (chỉ trả về pageId)
export interface CreateChapterPageEntryResponseDto {
  pageId: string;
} 