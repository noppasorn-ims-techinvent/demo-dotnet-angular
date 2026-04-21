/** ค่าตรงกับ backend OrderStatus */
export function orderStatusLabelTh(status: number): string {
  switch (status) {
    case 0:
      return 'รอชำระเงิน';
    case 1:
      return 'ชำระแล้ว';
    case 2:
      return 'จัดส่งแล้ว';
    case 3:
      return 'ยกเลิก';
    case 4:
      return 'รอยกเลิก (รอพิจารณา)';
    default:
      return String(status);
  }
}
