import api from "./axiosInstance";

const unwrap = (res) => res?.data?.data ?? res?.data ?? res;

export const adminStatisticsService = {
  getStatistics: async ({ from, to, departmentId } = {}) => {
    const params = {};
    if (from) params.from = from;
    if (to) params.to = to;
    if (departmentId != null && departmentId !== "" && departmentId !== 0) {
      params.departmentId = departmentId;
    }
    const res = await api.get("/admin/statistics", { params });
    return unwrap(res);
  },
};
