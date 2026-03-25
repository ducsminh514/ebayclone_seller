/// <reference types="node" />

export const API_BASE = process.env.API_BASE_URL ?? 'https://localhost:7250';


export const routes = {
  home: '/',
  login: '/login',
  register: '/register',
  verifyEmail: '/verify-email',
  onboarding: '/onboarding',
  listings: '/listings',
  listingsStart: '/listings/start',
  listingsCreate: '/listings/create',
  listingsInventory: '/listings/inventory',
  orders: '/orders',
  bpManage: '/bp/manage',
  bpPaymentCreate: '/bp/pmt/create',
  bpShippingCreate: '/bp/shp/create',
  bpReturnCreate: '/bp/rtn/create',
  vouchers: '/marketing/vouchers',
  vouchersCreate: '/marketing/vouchers/create',
  wallet: '/seller/wallet',
  feedback: '/feedback',
  adminCategories: '/admin/categories',
} as const;
