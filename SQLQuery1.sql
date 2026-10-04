-- 1. Delete the dependent records first
DELETE FROM dbo.Memberships WHERE MemberId = 5;

-- 2. Delete the primary record
DELETE FROM dbo.MemberProfiles WHERE MemberId = 5;
