export type ContributionStatus = 1 | 3 | 4;
export const ContributionStatusPending: ContributionStatus = 1;
export const ContributionStatusPaid: ContributionStatus = 3;
export const ContributionStatusRefused: ContributionStatus = 4;

export interface AdminContribution {
  id: string;
  amount: number;
  currency: string;
  status: ContributionStatus;
  contributorName: string | null;
  contributorEmail: string | null;
  message: string | null;
  asaasPaymentId: string | null;
  paidAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CheckoutContributionPixResult {
  pixKey: string;
  pixKeyType: string;
  beneficiary: string;
  bank: string | null;
  amount: number;
  qrCodePayload: string;
  qrCodeImageBase64: string;
}

export interface ConfirmContributionPixPayload {
  amount: number;
  name: string;
  email: string;
  message: string | null;
}

export interface CheckoutContributionCardPayload {
  amount: number;
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

export interface ConfirmContributionResult {
  contributionId: string;
  status: 'Confirmed' | 'Pending' | 'Refused';
}

export interface CheckoutContributionCardResult {
  contributionId: string;
  asaasPaymentId: string;
  status: 'Confirmed' | 'Pending' | 'Refused';
}
