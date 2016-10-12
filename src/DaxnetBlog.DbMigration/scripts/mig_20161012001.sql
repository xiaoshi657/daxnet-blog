USE [DaxnetBlogDB]
GO

IF OBJECT_ID('Replies', 'U') IS NOT NULL 
  DROP TABLE [Replies]; 
  
/****** Object:  Table [dbo].[Replies]    Script Date: 2016/10/12 12:02:25 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Replies](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BlogPostId] [int] NOT NULL,
	[AccountId] [int] NOT NULL,
	[DatePublished] [datetime] NOT NULL,
	[ParentId] [int] NULL,
	[Content] [ntext] NOT NULL,
 CONSTRAINT [PK_Replies] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

ALTER TABLE [dbo].[Replies]  WITH CHECK ADD  CONSTRAINT [FK_Replies_Accounts] FOREIGN KEY([AccountId])
REFERENCES [dbo].[Accounts] ([Id])
GO

ALTER TABLE [dbo].[Replies] CHECK CONSTRAINT [FK_Replies_Accounts]
GO

ALTER TABLE [dbo].[Replies]  WITH CHECK ADD  CONSTRAINT [FK_Replies_BlogPosts] FOREIGN KEY([BlogPostId])
REFERENCES [dbo].[BlogPosts] ([Id])
GO

ALTER TABLE [dbo].[Replies] CHECK CONSTRAINT [FK_Replies_BlogPosts]
GO

ALTER TABLE [dbo].[Replies]  WITH CHECK ADD  CONSTRAINT [FK_Replies_Replies1] FOREIGN KEY([ParentId])
REFERENCES [dbo].[Replies] ([Id])
GO

ALTER TABLE [dbo].[Replies] CHECK CONSTRAINT [FK_Replies_Replies1]
GO


