import axiosInstance from "./axiosInstance";

export const announcementService = {
  getManageAnnouncements: () => axiosInstance.get("/Announcement/manage"),

  // payload must be a FormData when using Image (multipart/form-data)
  createAnnouncement: (payload) =>
    axiosInstance.post("/Announcement", payload, {
      headers: { "Content-Type": "multipart/form-data" },
    }),

  // backend Update does not accept image currently (uses UpdateAnnouncementDto with JSON)
  updateAnnouncement: (id, payload) => axiosInstance.put(`/Announcement/${id}`, payload),
  deleteAnnouncement: (id) => axiosInstance.delete(`/Announcement/${id}`),
};
