/****** Script for SelectTopNRows command from SSMS  ******/
SELECT TOP (1000) [Id]
      ,[GroupMemberId]
      ,[GroupId]
      ,[GroupRoleId]
      ,[GroupRoleName]
      ,[IsLeader]
      ,[GroupMemberStatus]
      ,[IsArchived]
      ,[ArchivedDateTime]
      ,[ArchivedByPersonAliasId]
      ,[InactiveDateTime]
      ,[EffectiveDateTime]
      ,[ExpireDateTime]
      ,[CurrentRowIndicator]
      ,[CreatedDateTime]
      ,[ModifiedDateTime]
      ,[CreatedByPersonAliasId]
      ,[ModifiedByPersonAliasId]
      ,[Guid]
      ,[ForeignId]
      ,[ForeignGuid]
      ,[ForeignKey]
  FROM [RockRMS-develop-June18].[dbo].[GroupMemberHistorical]


declare @personId int = (select top 1 Id from Person where NickName = 'Ted' and LastName = 'Decker') /*Get PersonId for Ted Decker*/

delete from GroupMemberHistorical where GroupMemberId in (select Id from GroupMember where PersonId = @personId)

  INSERT INTO [GroupMemberHistorical]
  ([Guid], [GroupMemberId], [GroupId], [GroupRoleId], [GroupRoleName], [IsLeader], [GroupMemberStatus], [IsArchived], [EffectiveDateTime] ,[ExpireDateTime],[CurrentRowIndicator])
  VALUES
  (newid(), 80, 108,19, 'Member', 0, 1, 0, '1/1/2017', '4/5/2018', 1)

    INSERT INTO [GroupMemberHistorical]
  ([Guid], [GroupMemberId], [GroupId], [GroupRoleId], [GroupRoleName], [IsLeader], [GroupMemberStatus], [IsArchived], [EffectiveDateTime] ,[ExpireDateTime],[CurrentRowIndicator])
  VALUES
  (newid(), 160, 57,19, 'Member', 0, 1, 0, '4/7/2018', '9999-01-01 00:00:00.000', 1)

  INSERT INTO [GroupMemberHistorical]
  ([Guid], [GroupMemberId], [GroupId], [GroupRoleId], [GroupRoleName], [IsLeader], [GroupMemberStatus], [IsArchived], [EffectiveDateTime] ,[ExpireDateTime],[CurrentRowIndicator])
  VALUES
  (newid(), 82, 109,24, 'Leader', 1, 1, 0, '1/1/2017', '1/5/2018', 0)

    INSERT INTO [GroupMemberHistorical]
  ([Guid], [GroupMemberId], [GroupId], [GroupRoleId], [GroupRoleName], [IsLeader], [GroupMemberStatus], [IsArchived], [EffectiveDateTime] ,[ExpireDateTime],[CurrentRowIndicator])
  VALUES
  (newid(), 82, 109,24, 'Leader', 1, 1, 0, '2/1/2018', '9999-01-01 00:00:00.000', 1)