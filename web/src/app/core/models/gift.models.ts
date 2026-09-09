export type GiftStatus = 1 | 3 | 4;
export const GiftStatusAvailable: GiftStatus = 1;
export const GiftStatusPaid: GiftStatus = 3;
export const GiftStatusCanceled: GiftStatus = 4;

export interface Gift {
  id: string;
  title: string;
  description: string;
  imageUrl: string | null;
  price: number;
  currency: string;
  status: GiftStatus;
}

export interface AdminGift extends Gift {
  imageBlobName: string | null;
  buyerName: string | null;
  buyerEmail: string | null;
  buyerMessage: string | null;
  asaasPaymentId: string | null;
  paidAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CheckoutPixResult {
  giftId: string;
  pixKey: string;
  pixKeyType: string;
  beneficiary: string;
  bank: string | null;
  amount: number;
  qrCodePayload: string;
  qrCodeImageBase64: string;
}

export interface ConfirmPixPayload {
  name: string;
  email: string;
  message: string | null;
}

export interface CheckoutCardPayload {
  name: string;
  email: string;
  document: string;
  phone: string;
  postalCode: string;
  addressNumber: string;
  message: string | null;
  card: {
    holderName: string;
    number: string;
    expiryMonth: string;
    expiryYear: string;
    ccv: string;
  };
}

export interface CheckoutCardResult {
  giftId: string;
  asaasPaymentId: string;
  status: 'Confirmed' | 'Pending' | 'Refused';
}

export interface UploadedImage {
  blobName: string;
  url: string;
}

export interface CreateOrUpdateGiftPayload {
  title: string;
  description: string;
  price: number;
  imageBlobName: string | null;
}
