using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using LockLock.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace LockLock.Controllers
{
    public class BookingRoomController : Controller
    {
        private FirestoreDb firestoreDb;
        public BookingRoomController()
        {
            firestoreDb = FirestoreDb.Create("locklock-47b1d");
        }

        public async Task<IActionResult> IndexAsync(string room)
        {
            Tuple<string, string, string> adminUID = await verifyAdminTokenAsync();
            if (adminUID != null)
            {
                TempData["name"] = adminUID.Item2;
                TempData["surname"] = adminUID.Item3;

                List<BookingModel> bookingList = new List<BookingModel>();

                Query transactionQuery = firestoreDb.Collection("transaction").WhereEqualTo("roomID", room);
                QuerySnapshot transactionQuerySnapshot = await transactionQuery.GetSnapshotAsync();

                DateTime currentDate = DateTime.Now;

                foreach (DocumentSnapshot transactionSnapshot in transactionQuerySnapshot)
                {
                    if (transactionSnapshot.Exists)
                    {
                        TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                        Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("transactionID", transactionSnapshot.Id);
                        QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                        List<DateTime> timeLists = new List<DateTime>();
                        foreach (DocumentSnapshot borrowSnapshot in borrowQuerySnapshot)
                        {
                            BorrowModel borrowData = borrowSnapshot.ConvertTo<BorrowModel>();
                            timeLists.Add(borrowData.time.ToLocalTime());
                        }
                        timeLists.Sort();
                        int lastIndex = timeLists.Count - 1;
                        int timeCompare = 0;
                        if (lastIndex >= 0)
                        {
                            timeCompare = DateTime.Compare(timeLists[timeLists.Count - 1], currentDate);
                        }

                        DocumentReference roomReference = firestoreDb.Collection("room").Document(transactionData.roomID);
                        DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                        RoomModel roomData = roomSnapshot.ConvertTo<RoomModel>();

                        DocumentReference userReference = firestoreDb.Collection("users").Document(transactionData.userID);
                        DocumentSnapshot userSnapshot = await userReference.GetSnapshotAsync();
                        UserModel userData = userSnapshot.ConvertTo<UserModel>();

                        Query blacklistQuery = firestoreDb.Collection("blacklist").WhereEqualTo("userID", userReference.Id);
                        QuerySnapshot blacklistQuerySnapshot = await blacklistQuery.GetSnapshotAsync();

                        BookingModel bookingItem = new BookingModel()
                        {
                            BookingID = transactionSnapshot.Id,
                            Name = roomData.objName,
                            Num = 1,
                            RoomName = roomData.name,
                            timeList = timeLists,
                            status = timeCompare > 0 ? "Complete" : transactionData.cancel ? "Cancel" : "Booking",
                            cancel = timeCompare > 0 ? false : !transactionData.cancel,
                            inBlacklist = blacklistQuerySnapshot.Count == 0,
                            name = userData.Firstname + " " + userData.Lastname,
                            userID = transactionData.userID,
                            RoomID = roomSnapshot.Id
                        };
                        bookingList.Add(bookingItem);
                    }
                    else
                    {
                        Console.WriteLine("Document does not exist!", transactionSnapshot.Id);
                    }
                }
                return View(bookingList);
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }

        }

        public async Task<IActionResult> cancelAsync(string transactionID)
        {
            Tuple<string, string, string> adminUID = await verifyAdminTokenAsync();
            if (adminUID != null)
            {
                DocumentReference transactionReference = firestoreDb.Collection("transaction").Document(transactionID);
                DocumentSnapshot transactionSnapshot = await transactionReference.GetSnapshotAsync();
                TransactionModel transactionData = transactionSnapshot.ConvertTo<TransactionModel>();

                DocumentReference roomReference = firestoreDb.Collection("room").Document(transactionData.roomID);
                DocumentSnapshot roomSnapshot = await roomReference.GetSnapshotAsync();
                RoomModel roomData = roomSnapshot.ConvertTo<RoomModel>();

                if (roomData.adminID == adminUID.Item1)
                {
                    await transactionReference.UpdateAsync("cancel", true);

                    Query borrowQuery = firestoreDb.Collection("borrow").WhereEqualTo("transactionID", transactionSnapshot.Id);
                    QuerySnapshot borrowQuerySnapshot = await borrowQuery.GetSnapshotAsync();
                    foreach (DocumentSnapshot borrowSnapshot in borrowQuerySnapshot)
                    {
                        DocumentReference borrowReference = firestoreDb.Collection("borrow").Document(borrowSnapshot.Id);
                        await borrowReference.UpdateAsync("cancel", true);
                    }
                    return Ok();
                }
                else
                {
                    Console.WriteLine("adminID not macth");
                    return Unauthorized();
                }
            }
            else
            {
                return RedirectToAction("SignIn", "Account");
            }
        }




        private async Task<Tuple<string, string, string>> verifyAdminTokenAsync()
        {
            try
            {
                var token = HttpContext.Session.GetString("_UserToken");
                FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                DocumentReference userReference = firestoreDb.Collection("users").Document(decodedToken.Uid);
                DocumentSnapshot userSnapshot = await userReference.GetSnapshotAsync();
                UserModel user = userSnapshot.ConvertTo<UserModel>();

                if (user.role == "admin")
                {
                    return new Tuple<string, string, string>(decodedToken.Uid, user.Firstname, user.Lastname);
                }
                else
                {
                    Console.WriteLine("User role");
                    return null;
                }
            }
            catch
            {
                Console.WriteLine("ID token must not be null or empty");
                return null;
            }
        }
    }
}