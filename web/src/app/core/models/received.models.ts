export type ReceivedType = 'gift' | 'contribution';
export type PaymentMethod = 'pix' | 'card';

export interface ReceivedItem {
  type: ReceivedType;
  id: string;
  title: string;
  amount: number;
  currency: string;
  contributorName: string | null;
  contributorEmail: string | null;
  message: string | null;
  asaasPaymentId: string | null;
  paymentMethod: PaymentMethod;
  paidAt: string;
}
