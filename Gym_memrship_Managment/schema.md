# Database Schema Documentation

### AspNetRoles

**Primary Key** : Id<br>
**Foreign Key** : -<br>
**Description** : Stores information and records for AspNetRoles in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| Id | nvarchar | 450 | NOT NULL | Stores id data |
| Name | nvarchar | 256 | NULL | Stores name data |
| NormalizedName | nvarchar | 256 | NULL | Stores normalizedname data |
| ConcurrencyStamp | nvarchar | MAX | NULL | Stores concurrencystamp data |

<br>

### AspNetUsers

**Primary Key** : Id<br>
**Foreign Key** : -<br>
**Description** : Stores information and records for AspNetUsers in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| Id | nvarchar | 450 | NOT NULL | Stores id data |
| FullName | nvarchar | 100 | NOT NULL | Stores fullname data |
| IsActive | bit | - | NOT NULL | Stores isactive data |
| ProfileImagePath | nvarchar | MAX | NULL | Stores profileimagepath data |
| CreatedAt | datetime2 | - | NOT NULL | Stores createdat data |
| UpdatedAt | datetime2 | - | NOT NULL | Stores updatedat data |
| UserName | nvarchar | 256 | NULL | Stores username data |
| NormalizedUserName | nvarchar | 256 | NULL | Stores normalizedusername data |
| Email | nvarchar | 256 | NULL | Stores email data |
| NormalizedEmail | nvarchar | 256 | NULL | Stores normalizedemail data |
| EmailConfirmed | bit | - | NOT NULL | Stores emailconfirmed data |
| PasswordHash | nvarchar | MAX | NULL | Stores passwordhash data |
| SecurityStamp | nvarchar | MAX | NULL | Stores securitystamp data |
| ConcurrencyStamp | nvarchar | MAX | NULL | Stores concurrencystamp data |
| PhoneNumber | nvarchar | MAX | NULL | Stores phonenumber data |
| PhoneNumberConfirmed | bit | - | NOT NULL | Stores phonenumberconfirmed data |
| TwoFactorEnabled | bit | - | NOT NULL | Stores twofactorenabled data |
| LockoutEnd | datetimeoffset | - | NULL | Stores lockoutend data |
| LockoutEnabled | bit | - | NOT NULL | Stores lockoutenabled data |
| AccessFailedCount | int | - | NOT NULL | Stores accessfailedcount data |

<br>

### AuditLogs

**Primary Key** : AuditLogId<br>
**Foreign Key** : -<br>
**Description** : Stores information and records for AuditLogs in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| AuditLogId | int | - | NOT NULL, IDENTITY | Stores auditlogid data |
| UserId | nvarchar | MAX | NULL | Stores userid data |
| UserEmail | nvarchar | 100 | NULL | Stores useremail data |
| Action | nvarchar | 100 | NOT NULL | Stores action data |
| EntityName | nvarchar | 100 | NULL | Stores entityname data |
| EntityId | nvarchar | 50 | NULL | Stores entityid data |
| Description | nvarchar | 1000 | NULL | Stores description data |
| Timestamp | datetime2 | - | NOT NULL | Stores timestamp data |
| IPAddress | nvarchar | 50 | NULL | Stores ipaddress data |
| Metadata | nvarchar | MAX | NULL | Stores metadata data |

<br>

### MembershipPlans

**Primary Key** : PlanId<br>
**Foreign Key** : -<br>
**Description** : Stores information and records for MembershipPlans in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| PlanId | int | - | NOT NULL, IDENTITY | Stores planid data |
| PlanName | nvarchar | 100 | NOT NULL | Stores planname data |
| Description | nvarchar | 500 | NULL | Stores description data |
| DurationInDays | int | - | NOT NULL | Stores durationindays data |
| Price | decimal | 18 | NOT NULL | Stores price data |
| RegistrationFee | decimal | 18 | NOT NULL | Stores registrationfee data |
| RenewalFee | decimal | 18 | NOT NULL | Stores renewalfee data |
| LateFeePerDay | decimal | 18 | NOT NULL | Stores latefeeperday data |
| GracePeriodDays | int | - | NOT NULL | Stores graceperioddays data |
| IsActive | bit | - | NOT NULL | Stores isactive data |
| IsDeleted | bit | - | NOT NULL | Stores isdeleted data |
| CreatedAt | datetime2 | - | NOT NULL | Stores createdat data |
| UpdatedAt | datetime2 | - | NOT NULL | Stores updatedat data |

<br>

### SystemSettings

**Primary Key** : SettingId<br>
**Foreign Key** : -<br>
**Description** : Stores information and records for SystemSettings in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| SettingId | int | - | NOT NULL, IDENTITY | Stores settingid data |
| Key | nvarchar | 100 | NOT NULL | Stores key data |
| Value | nvarchar | 1000 | NULL | Stores value data |
| Description | nvarchar | 300 | NULL | Stores description data |
| UpdatedAt | datetime2 | - | NOT NULL | Stores updatedat data |

<br>

### Trainers

**Primary Key** : TrainerId<br>
**Foreign Key** : -<br>
**Description** : Stores information and records for Trainers in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| TrainerId | int | - | NOT NULL, IDENTITY | Stores trainerid data |
| FullName | nvarchar | 100 | NOT NULL | Stores fullname data |
| Phone | nvarchar | 15 | NOT NULL | Stores phone data |
| Email | nvarchar | 150 | NULL | Stores email data |
| Specialization | nvarchar | 200 | NULL | Stores specialization data |
| ExperienceYears | int | - | NOT NULL | Stores experienceyears data |
| JoiningDate | datetime2 | - | NOT NULL | Stores joiningdate data |
| ProfileImage | nvarchar | MAX | NULL | Stores profileimage data |
| Status | int | - | NOT NULL | Stores status data |
| IsDeleted | bit | - | NOT NULL | Stores isdeleted data |

<br>

### AspNetRoleClaims

**Primary Key** : Id<br>
**Foreign Key** : RoleId<br>
**Description** : Stores information and records for AspNetRoleClaims in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| Id | int | - | NOT NULL, IDENTITY | Stores id data |
| RoleId | nvarchar | 450 | NOT NULL | Stores roleid data |
| ClaimType | nvarchar | MAX | NULL | Stores claimtype data |
| ClaimValue | nvarchar | MAX | NULL | Stores claimvalue data |

<br>

### AspNetUserClaims

**Primary Key** : Id<br>
**Foreign Key** : UserId<br>
**Description** : Stores information and records for AspNetUserClaims in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| Id | int | - | NOT NULL, IDENTITY | Stores id data |
| UserId | nvarchar | 450 | NOT NULL | Stores userid data |
| ClaimType | nvarchar | MAX | NULL | Stores claimtype data |
| ClaimValue | nvarchar | MAX | NULL | Stores claimvalue data |

<br>

### AspNetUserLogins

**Primary Key** : -<br>
**Foreign Key** : UserId<br>
**Description** : Stores information and records for AspNetUserLogins in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| LoginProvider | nvarchar | 450 | NOT NULL | Stores loginprovider data |
| ProviderKey | nvarchar | 450 | NOT NULL | Stores providerkey data |
| ProviderDisplayName | nvarchar | MAX | NULL | Stores providerdisplayname data |
| UserId | nvarchar | 450 | NOT NULL | Stores userid data |

<br>

### AspNetUserRoles

**Primary Key** : -<br>
**Foreign Key** : RoleId, UserId<br>
**Description** : Stores information and records for AspNetUserRoles in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| UserId | nvarchar | 450 | NOT NULL | Stores userid data |
| RoleId | nvarchar | 450 | NOT NULL | Stores roleid data |

<br>

### AspNetUserTokens

**Primary Key** : -<br>
**Foreign Key** : UserId<br>
**Description** : Stores information and records for AspNetUserTokens in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| UserId | nvarchar | 450 | NOT NULL | Stores userid data |
| LoginProvider | nvarchar | 450 | NOT NULL | Stores loginprovider data |
| Name | nvarchar | 450 | NOT NULL | Stores name data |
| Value | nvarchar | MAX | NULL | Stores value data |

<br>

### MemberProfiles

**Primary Key** : MemberId<br>
**Foreign Key** : UserId<br>
**Description** : Stores information and records for MemberProfiles in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| MemberId | int | - | NOT NULL, IDENTITY | Stores memberid data |
| MembershipNumber | nvarchar | 20 | NOT NULL | Stores membershipnumber data |
| QrCodeHash | nvarchar | 64 | NOT NULL | Stores qrcodehash data |
| UserId | nvarchar | 450 | NULL | Stores userid data |
| FullName | nvarchar | 100 | NOT NULL | Stores fullname data |
| Gender | int | - | NOT NULL | Stores gender data |
| DateOfBirth | datetime2 | - | NULL | Stores dateofbirth data |
| Phone | nvarchar | 15 | NOT NULL | Stores phone data |
| Email | nvarchar | 150 | NOT NULL | Stores email data |
| Address | nvarchar | 250 | NULL | Stores address data |
| EmergencyContactName | nvarchar | 100 | NULL | Stores emergencycontactname data |
| EmergencyContactPhone | nvarchar | 15 | NULL | Stores emergencycontactphone data |
| JoinDate | datetime2 | - | NOT NULL | Stores joindate data |
| ProfileImage | nvarchar | MAX | NULL | Stores profileimage data |
| Status | int | - | NOT NULL | Stores status data |
| IsDeleted | bit | - | NOT NULL | Stores isdeleted data |
| CreatedAt | datetime2 | - | NOT NULL | Stores createdat data |
| UpdatedAt | datetime2 | - | NOT NULL | Stores updatedat data |

<br>

### Notifications

**Primary Key** : NotificationId<br>
**Foreign Key** : UserId<br>
**Description** : Stores information and records for Notifications in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| NotificationId | int | - | NOT NULL, IDENTITY | Stores notificationid data |
| UserId | nvarchar | 450 | NOT NULL | Stores userid data |
| Title | nvarchar | 200 | NOT NULL | Stores title data |
| Message | nvarchar | 1000 | NOT NULL | Stores message data |
| Type | int | - | NOT NULL | Stores type data |
| IsRead | bit | - | NOT NULL | Stores isread data |
| CreatedAt | datetime2 | - | NOT NULL | Stores createdat data |
| ReadAt | datetime2 | - | NULL | Stores readat data |

<br>

### StaffProfiles

**Primary Key** : StaffProfileId<br>
**Foreign Key** : UserId<br>
**Description** : Stores information and records for StaffProfiles in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| StaffProfileId | int | - | NOT NULL, IDENTITY | Stores staffprofileid data |
| UserId | nvarchar | 450 | NOT NULL | Stores userid data |
| EmployeeCode | nvarchar | 20 | NOT NULL | Stores employeecode data |
| Designation | nvarchar | 100 | NOT NULL | Stores designation data |
| JoiningDate | datetime2 | - | NOT NULL | Stores joiningdate data |
| IsActive | bit | - | NOT NULL | Stores isactive data |

<br>

### Batches

**Primary Key** : BatchId<br>
**Foreign Key** : TrainerId<br>
**Description** : Stores information and records for Batches in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| BatchId | int | - | NOT NULL, IDENTITY | Stores batchid data |
| BatchName | nvarchar | 100 | NOT NULL | Stores batchname data |
| Description | nvarchar | 500 | NULL | Stores description data |
| TrainerId | int | - | NULL | Stores trainerid data |
| StartTime | time | - | NOT NULL | Stores starttime data |
| EndTime | time | - | NOT NULL | Stores endtime data |
| MaximumCapacity | int | - | NOT NULL | Stores maximumcapacity data |
| Location | nvarchar | 200 | NULL | Stores location data |
| IsActive | bit | - | NOT NULL | Stores isactive data |
| IsDeleted | bit | - | NOT NULL | Stores isdeleted data |
| CreatedAt | datetime2 | - | NOT NULL | Stores createdat data |
| UpdatedAt | datetime2 | - | NOT NULL | Stores updatedat data |

<br>

### TrainerSlots

**Primary Key** : TrainerSlotId<br>
**Foreign Key** : TrainerId<br>
**Description** : Stores information and records for TrainerSlots in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| TrainerSlotId | int | - | NOT NULL, IDENTITY | Stores trainerslotid data |
| TrainerId | int | - | NOT NULL | Stores trainerid data |
| DayOfWeek | int | - | NOT NULL | Stores dayofweek data |
| StartTime | time | - | NOT NULL | Stores starttime data |
| EndTime | time | - | NOT NULL | Stores endtime data |
| IsActive | bit | - | NOT NULL | Stores isactive data |

<br>

### Memberships

**Primary Key** : MembershipId<br>
**Foreign Key** : MemberId, PlanId<br>
**Description** : Stores information and records for Memberships in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| MembershipId | int | - | NOT NULL, IDENTITY | Stores membershipid data |
| MemberId | int | - | NOT NULL | Stores memberid data |
| PlanId | int | - | NOT NULL | Stores planid data |
| StartDate | datetime2 | - | NOT NULL | Stores startdate data |
| EndDate | datetime2 | - | NOT NULL | Stores enddate data |
| BaseAmount | decimal | 18 | NOT NULL | Stores baseamount data |
| Discount | decimal | 18 | NOT NULL | Stores discount data |
| FinalAmount | decimal | 18 | NOT NULL | Stores finalamount data |
| PaidAmount | decimal | 18 | NOT NULL | Stores paidamount data |
| DueAmount | decimal | 18 | NOT NULL | Stores dueamount data |
| RenewalCount | int | - | NOT NULL | Stores renewalcount data |
| OverrideStatus | int | - | NULL | Stores overridestatus data |
| CreatedAt | datetime2 | - | NOT NULL | Stores createdat data |
| UpdatedAt | datetime2 | - | NOT NULL | Stores updatedat data |

<br>

### Attendances

**Primary Key** : AttendanceId<br>
**Foreign Key** : BatchId, MemberId<br>
**Description** : Stores information and records for Attendances in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| AttendanceId | int | - | NOT NULL, IDENTITY | Stores attendanceid data |
| MemberId | int | - | NOT NULL | Stores memberid data |
| BatchId | int | - | NULL | Stores batchid data |
| Date | datetime2 | - | NOT NULL | Stores date data |
| CheckInTime | datetime2 | - | NOT NULL | Stores checkintime data |
| CheckOutTime | datetime2 | - | NULL | Stores checkouttime data |
| DurationMinutes | int | - | NULL | Stores durationminutes data |
| Status | int | - | NOT NULL | Stores status data |
| Notes | nvarchar | 200 | NULL | Stores notes data |

<br>

### BatchEnrollments

**Primary Key** : BatchEnrollmentId<br>
**Foreign Key** : BatchId, MemberId<br>
**Description** : Stores information and records for BatchEnrollments in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| BatchEnrollmentId | int | - | NOT NULL, IDENTITY | Stores batchenrollmentid data |
| MemberId | int | - | NOT NULL | Stores memberid data |
| BatchId | int | - | NOT NULL | Stores batchid data |
| EnrolledDate | datetime2 | - | NOT NULL | Stores enrolleddate data |
| TransferredDate | datetime2 | - | NULL | Stores transferreddate data |
| Status | int | - | NOT NULL | Stores status data |

<br>

### BatchSchedules

**Primary Key** : BatchScheduleId<br>
**Foreign Key** : BatchId<br>
**Description** : Stores information and records for BatchSchedules in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| BatchScheduleId | int | - | NOT NULL, IDENTITY | Stores batchscheduleid data |
| BatchId | int | - | NOT NULL | Stores batchid data |
| DayOfWeek | int | - | NOT NULL | Stores dayofweek data |
| StartTime | time | - | NOT NULL | Stores starttime data |
| EndTime | time | - | NOT NULL | Stores endtime data |

<br>

### PTBookings

**Primary Key** : BookingId<br>
**Foreign Key** : MemberId, TrainerSlotId<br>
**Description** : Stores information and records for PTBookings in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| BookingId | int | - | NOT NULL, IDENTITY | Stores bookingid data |
| MemberId | int | - | NOT NULL | Stores memberid data |
| TrainerSlotId | int | - | NOT NULL | Stores trainerslotid data |
| BookingDate | datetime2 | - | NOT NULL | Stores bookingdate data |
| Status | nvarchar | 20 | NOT NULL | Stores status data |
| CreatedAt | datetime2 | - | NOT NULL | Stores createdat data |

<br>

### Fines

**Primary Key** : FineId<br>
**Foreign Key** : MemberId, MembershipId<br>
**Description** : Stores information and records for Fines in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| FineId | int | - | NOT NULL, IDENTITY | Stores fineid data |
| MemberId | int | - | NOT NULL | Stores memberid data |
| MembershipId | int | - | NULL | Stores membershipid data |
| Reason | nvarchar | 300 | NOT NULL | Stores reason data |
| Amount | decimal | 18 | NOT NULL | Stores amount data |
| DateIssued | datetime2 | - | NOT NULL | Stores dateissued data |
| Status | int | - | NOT NULL | Stores status data |
| PaidDate | datetime2 | - | NULL | Stores paiddate data |
| Notes | nvarchar | 300 | NULL | Stores notes data |

<br>

### Payments

**Primary Key** : PaymentId<br>
**Foreign Key** : MemberId, MembershipId<br>
**Description** : Stores information and records for Payments in the system.

| Field Name | Data Type | Size | Constraints | Description |
|---|---|---|---|---|
| PaymentId | int | - | NOT NULL, IDENTITY | Stores paymentid data |
| ReceiptNumber | nvarchar | 30 | NOT NULL | Stores receiptnumber data |
| MemberId | int | - | NOT NULL | Stores memberid data |
| MembershipId | int | - | NULL | Stores membershipid data |
| PaymentDate | datetime2 | - | NOT NULL | Stores paymentdate data |
| Amount | decimal | 18 | NOT NULL | Stores amount data |
| PaymentMethod | int | - | NOT NULL | Stores paymentmethod data |
| PaymentType | int | - | NOT NULL | Stores paymenttype data |
| TransactionReference | nvarchar | 100 | NULL | Stores transactionreference data |
| Notes | nvarchar | 500 | NULL | Stores notes data |
| ReceivedBy | nvarchar | 100 | NULL | Stores receivedby data |
| CreatedAt | datetime2 | - | NOT NULL | Stores createdat data |

<br>

