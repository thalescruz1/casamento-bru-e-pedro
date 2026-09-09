export type Attendance = 1 | 2;
export const AttendanceSim: Attendance = 1;
export const AttendanceNao: Attendance = 2;

export interface SubmitRsvpPayload {
  name: string;
  email: string;
  phone: string;
  attend: Attendance;
  guests: number;
  guestNames: string | null;
  restrictions: string | null;
}

export interface RsvpConfirmation {
  id: string;
  name: string;
  attend: Attendance;
  submittedAt: string;
}

export interface RsvpListItem {
  id: string;
  name: string;
  email: string;
  phone: string | null;
  attend: Attendance;
  guests: number;
  guestNames: string | null;
  restrictions: string | null;
  submittedAt: string;
}

export interface RsvpList {
  items: RsvpListItem[];
  total: number;
}
