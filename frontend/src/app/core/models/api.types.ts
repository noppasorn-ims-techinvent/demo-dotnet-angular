export interface UserDto {
  id: number;
  email: string;
  displayName: string;
  roles: string[];
}

export interface AuthResponse {
  token: string;
  expiresAtUtc: string;
  user: UserDto;
}

export interface ProductDto {
  id: number;
  sellerId: number;
  sellerDisplayName: string;
  name: string;
  description?: string | null;
  price: number;
  stockQuantity: number;
  createdAtUtc: string;
}

export interface OrderLineDto {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface OrderDto {
  id: number;
  buyerId: number;
  /** Matches backend `OrderStatus` enum numeric value. */
  status: number;
  totalAmount: number;
  createdAtUtc: string;
  lines: OrderLineDto[];
  preCancellationStatus?: number | null;
  buyerCancellationReason?: string | null;
  cancellationReviewerNote?: string | null;
  cancellationReviewedByUserId?: number | null;
  simulatedPaymentMethod?: string | null;
}

export interface AppConfigDto {
  apiBaseUrl: string;
  appName: string;
}

export interface CartLine {
  product: ProductDto;
  quantity: number;
}
