CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [IsActive] bit NOT NULL,
    [ProfileImagePath] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AuditLogs] (
    [AuditLogId] int NOT NULL IDENTITY,
    [UserId] nvarchar(max) NULL,
    [UserEmail] nvarchar(100) NULL,
    [Action] nvarchar(100) NOT NULL,
    [EntityName] nvarchar(100) NULL,
    [EntityId] nvarchar(50) NULL,
    [Description] nvarchar(1000) NULL,
    [Timestamp] datetime2 NOT NULL,
    [IPAddress] nvarchar(50) NULL,
    [Metadata] nvarchar(max) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId])
);
GO


CREATE TABLE [MembershipPlans] (
    [PlanId] int NOT NULL IDENTITY,
    [PlanName] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [DurationInDays] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [RegistrationFee] decimal(18,2) NOT NULL,
    [RenewalFee] decimal(18,2) NOT NULL,
    [LateFeePerDay] decimal(18,2) NOT NULL,
    [GracePeriodDays] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MembershipPlans] PRIMARY KEY ([PlanId])
);
GO


CREATE TABLE [SystemSettings] (
    [SettingId] int NOT NULL IDENTITY,
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(1000) NULL,
    [Description] nvarchar(300) NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([SettingId])
);
GO


CREATE TABLE [Trainers] (
    [TrainerId] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Phone] nvarchar(15) NOT NULL,
    [Email] nvarchar(150) NULL,
    [Specialization] nvarchar(200) NULL,
    [ExperienceYears] int NOT NULL,
    [JoiningDate] datetime2 NOT NULL,
    [ProfileImage] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Trainers] PRIMARY KEY ([TrainerId])
);
GO


CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [MemberProfiles] (
    [MemberId] int NOT NULL IDENTITY,
    [MembershipNumber] nvarchar(20) NOT NULL,
    [QrCodeHash] nvarchar(64) NOT NULL,
    [UserId] nvarchar(450) NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Gender] int NOT NULL,
    [DateOfBirth] datetime2 NULL,
    [Phone] nvarchar(15) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [Address] nvarchar(250) NULL,
    [EmergencyContactName] nvarchar(100) NULL,
    [EmergencyContactPhone] nvarchar(15) NULL,
    [JoinDate] datetime2 NOT NULL,
    [ProfileImage] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MemberProfiles] PRIMARY KEY ([MemberId]),
    CONSTRAINT [FK_MemberProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [Notifications] (
    [NotificationId] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [Type] int NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ReadAt] datetime2 NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
    CONSTRAINT [FK_Notifications_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [StaffProfiles] (
    [StaffProfileId] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [EmployeeCode] nvarchar(20) NOT NULL,
    [Designation] nvarchar(100) NOT NULL,
    [JoiningDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_StaffProfiles] PRIMARY KEY ([StaffProfileId]),
    CONSTRAINT [FK_StaffProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Batches] (
    [BatchId] int NOT NULL IDENTITY,
    [BatchName] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [TrainerId] int NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [MaximumCapacity] int NOT NULL,
    [Location] nvarchar(200) NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Batches] PRIMARY KEY ([BatchId]),
    CONSTRAINT [FK_Batches_Trainers_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Trainers] ([TrainerId]) ON DELETE SET NULL
);
GO


CREATE TABLE [TrainerSlots] (
    [TrainerSlotId] int NOT NULL IDENTITY,
    [TrainerId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_TrainerSlots] PRIMARY KEY ([TrainerSlotId]),
    CONSTRAINT [FK_TrainerSlots_Trainers_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [Trainers] ([TrainerId]) ON DELETE CASCADE
);
GO


CREATE TABLE [Memberships] (
    [MembershipId] int NOT NULL IDENTITY,
    [MemberId] int NOT NULL,
    [PlanId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [BaseAmount] decimal(18,2) NOT NULL,
    [Discount] decimal(18,2) NOT NULL,
    [FinalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [DueAmount] decimal(18,2) NOT NULL,
    [RenewalCount] int NOT NULL,
    [OverrideStatus] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Memberships] PRIMARY KEY ([MembershipId]),
    CONSTRAINT [FK_Memberships_MemberProfiles_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [MemberProfiles] ([MemberId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Memberships_MembershipPlans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [MembershipPlans] ([PlanId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Attendances] (
    [AttendanceId] int NOT NULL IDENTITY,
    [MemberId] int NOT NULL,
    [BatchId] int NULL,
    [Date] datetime2 NOT NULL,
    [CheckInTime] datetime2 NOT NULL,
    [CheckOutTime] datetime2 NULL,
    [DurationMinutes] int NULL,
    [Status] int NOT NULL,
    [Notes] nvarchar(200) NULL,
    CONSTRAINT [PK_Attendances] PRIMARY KEY ([AttendanceId]),
    CONSTRAINT [FK_Attendances_Batches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [Batches] ([BatchId]) ON DELETE SET NULL,
    CONSTRAINT [FK_Attendances_MemberProfiles_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [MemberProfiles] ([MemberId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [BatchEnrollments] (
    [BatchEnrollmentId] int NOT NULL IDENTITY,
    [MemberId] int NOT NULL,
    [BatchId] int NOT NULL,
    [EnrolledDate] datetime2 NOT NULL,
    [TransferredDate] datetime2 NULL,
    [Status] int NOT NULL,
    CONSTRAINT [PK_BatchEnrollments] PRIMARY KEY ([BatchEnrollmentId]),
    CONSTRAINT [FK_BatchEnrollments_Batches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [Batches] ([BatchId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BatchEnrollments_MemberProfiles_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [MemberProfiles] ([MemberId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [BatchSchedules] (
    [BatchScheduleId] int NOT NULL IDENTITY,
    [BatchId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    CONSTRAINT [PK_BatchSchedules] PRIMARY KEY ([BatchScheduleId]),
    CONSTRAINT [FK_BatchSchedules_Batches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [Batches] ([BatchId]) ON DELETE CASCADE
);
GO


CREATE TABLE [PTBookings] (
    [BookingId] int NOT NULL IDENTITY,
    [MemberId] int NOT NULL,
    [TrainerSlotId] int NOT NULL,
    [BookingDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PTBookings] PRIMARY KEY ([BookingId]),
    CONSTRAINT [FK_PTBookings_MemberProfiles_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [MemberProfiles] ([MemberId]) ON DELETE CASCADE,
    CONSTRAINT [FK_PTBookings_TrainerSlots_TrainerSlotId] FOREIGN KEY ([TrainerSlotId]) REFERENCES [TrainerSlots] ([TrainerSlotId]) ON DELETE CASCADE
);
GO


CREATE TABLE [Fines] (
    [FineId] int NOT NULL IDENTITY,
    [MemberId] int NOT NULL,
    [MembershipId] int NULL,
    [Reason] nvarchar(300) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [DateIssued] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [PaidDate] datetime2 NULL,
    [Notes] nvarchar(300) NULL,
    CONSTRAINT [PK_Fines] PRIMARY KEY ([FineId]),
    CONSTRAINT [FK_Fines_MemberProfiles_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [MemberProfiles] ([MemberId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Fines_Memberships_MembershipId] FOREIGN KEY ([MembershipId]) REFERENCES [Memberships] ([MembershipId]) ON DELETE SET NULL
);
GO


CREATE TABLE [Payments] (
    [PaymentId] int NOT NULL IDENTITY,
    [ReceiptNumber] nvarchar(30) NOT NULL,
    [MemberId] int NOT NULL,
    [MembershipId] int NULL,
    [PaymentDate] datetime2 NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentMethod] int NOT NULL,
    [PaymentType] int NOT NULL,
    [TransactionReference] nvarchar(100) NULL,
    [Notes] nvarchar(500) NULL,
    [ReceivedBy] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
    CONSTRAINT [FK_Payments_MemberProfiles_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [MemberProfiles] ([MemberId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Payments_Memberships_MembershipId] FOREIGN KEY ([MembershipId]) REFERENCES [Memberships] ([MembershipId]) ON DELETE SET NULL
);
GO


CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO


CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO


CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO


CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO


CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO


CREATE INDEX [IX_Attendances_BatchId] ON [Attendances] ([BatchId]);
GO


CREATE UNIQUE INDEX [IX_Attendances_MemberId_Date] ON [Attendances] ([MemberId], [Date]);
GO


CREATE INDEX [IX_BatchEnrollments_BatchId] ON [BatchEnrollments] ([BatchId]);
GO


CREATE INDEX [IX_BatchEnrollments_MemberId_BatchId_Status] ON [BatchEnrollments] ([MemberId], [BatchId], [Status]);
GO


CREATE INDEX [IX_Batches_TrainerId] ON [Batches] ([TrainerId]);
GO


CREATE INDEX [IX_BatchSchedules_BatchId] ON [BatchSchedules] ([BatchId]);
GO


CREATE INDEX [IX_Fines_MemberId] ON [Fines] ([MemberId]);
GO


CREATE INDEX [IX_Fines_MembershipId] ON [Fines] ([MembershipId]);
GO


CREATE UNIQUE INDEX [IX_MemberProfiles_Email] ON [MemberProfiles] ([Email]);
GO


CREATE UNIQUE INDEX [IX_MemberProfiles_MembershipNumber] ON [MemberProfiles] ([MembershipNumber]);
GO


CREATE UNIQUE INDEX [IX_MemberProfiles_UserId] ON [MemberProfiles] ([UserId]) WHERE [UserId] IS NOT NULL;
GO


CREATE INDEX [IX_Memberships_MemberId] ON [Memberships] ([MemberId]);
GO


CREATE INDEX [IX_Memberships_PlanId] ON [Memberships] ([PlanId]);
GO


CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
GO


CREATE INDEX [IX_Payments_MemberId] ON [Payments] ([MemberId]);
GO


CREATE INDEX [IX_Payments_MembershipId] ON [Payments] ([MembershipId]);
GO


CREATE UNIQUE INDEX [IX_Payments_ReceiptNumber] ON [Payments] ([ReceiptNumber]);
GO


CREATE INDEX [IX_PTBookings_MemberId] ON [PTBookings] ([MemberId]);
GO


CREATE INDEX [IX_PTBookings_TrainerSlotId] ON [PTBookings] ([TrainerSlotId]);
GO


CREATE UNIQUE INDEX [IX_StaffProfiles_UserId] ON [StaffProfiles] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_SystemSettings_Key] ON [SystemSettings] ([Key]);
GO


CREATE INDEX [IX_TrainerSlots_TrainerId] ON [TrainerSlots] ([TrainerId]);
GO


