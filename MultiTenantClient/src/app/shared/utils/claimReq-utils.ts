export const claimReq = {
  adminOnly: (c: any) => c.role == "Admin",
  hasLibraryId: (c: any) => 'libraryID' in c,
  femaleAndTeacher: (c: any) => c.gender == "Female" && c.role == "Teacher",
  femaleAndBelow10 : (c: any) => c.gender == "Female" && parseInt(c.age) < 10
}