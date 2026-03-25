using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlogCore.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

[assembly: DoNotParallelize]

namespace BlogCore.DAL.Tests;

[TestClass]
public class BlogRepositoryTests : IntegrationTestBase
{
    #region 1. Testy podstawowe z instrukcji

    [TestMethod]
    public void AddPost_ValidData_IncreasesCountByOne()
    {
        // Arrange
        var newPost = DataGenerator.GetPostFaker().Generate();

        // Act
        _repository.AddPost(newPost);

        // Assert
        var posts = _repository.GetAllPosts();
        Assert.AreEqual(1, posts.Count());
    }

    [TestMethod]
    [ExpectedException(typeof(DbUpdateException))]
    public void AddPost_NullContent_ThrowsDbUpdateException()
    {
        // Arrange
        var invalidPost = new Post
        {
            Author = "Jan Kowalski",
            Content = null!
        };

        // Act
        _repository.AddPost(invalidPost);
    }

    [TestMethod]
    public void GetCommentsByPostId_ReturnsExactlyGeneratedAmount()
    {
        // Arrange
        var post = DataGenerator.GetPostFaker().Generate();
        _repository.AddPost(post);

        var comments = DataGenerator.GetCommentFaker(post.Id).Generate(3);
        foreach (var comment in comments)
        {
            _repository.AddComment(comment);
        }

        // Act
        var result = _repository.GetCommentsByPostId(post.Id);

        // Assert
        Assert.AreEqual(3, result.Count());
    }

    #endregion

    #region 2. Testy Postów (Stabilność)

    [TestMethod]
    public void GetAllPosts_EmptyDb_ReturnsZero()
    {
        // Act
        var result = _repository.GetAllPosts();

        // Assert
        Assert.AreEqual(0, result.Count());
    }

    [TestMethod]
    public void AddPost_LongContent_SavesCorrectly()
    {
        // Arrange
        var post = DataGenerator.GetPostFaker().Generate();
        post.Content = new Bogus.Faker().Lorem.Paragraphs(5);   // Random text

        // Act
        _repository.AddPost(post);

        // Assert
        var savedPost = _repository.GetAllPosts().First();
        Assert.AreEqual(post.Content, savedPost.Content);
    }

    [TestMethod]
    public void AddPost_SpecialCharactersInAuthor_SavesCorrectly()
    {
        // Arrange
        var post = DataGenerator.GetPostFaker().Generate();
        post.Author = "Zażółć Gęślą Jaźń 123!@#";

        // Act
        _repository.AddPost(post);

        // Assert
        var savedPost = _repository.GetAllPosts().First();
        Assert.AreEqual("Zażółć Gęślą Jaźń 123!@#", savedPost.Author);
    }

    #endregion

    #region 3. Testy Komentarzy i Relacji

    [TestMethod]
    public void AddComment_ValidData_IncreasesCountForPost()
    {
        // Arrange
        var post = DataGenerator.GetPostFaker().Generate();
        _repository.AddPost(post);
        var comment = DataGenerator.GetCommentFaker(post.Id).Generate();

        // Act
        _repository.AddComment(comment);

        // Assert
        var results = _repository.GetCommentsByPostId(post.Id);
        Assert.AreEqual(1, results.Count());
    }

    [TestMethod]
    public void GetCommentsByPostId_NonExistentPost_ReturnsEmpty()
    {
        // Act
        var results = _repository.GetCommentsByPostId(9999);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count());
    }

    [TestMethod]
    [ExpectedException(typeof(DbUpdateException))]
    public void AddComment_OrphanComment_ThrowsException()
    {
        // Arrange
        var orphanComment = DataGenerator.GetCommentFaker(9999).Generate();

        // Act
        _repository.AddComment(orphanComment);
    }

    [TestMethod]
    public void MultipleComments_DifferentPosts_ReturnsOnlyCorrectOnes()
    {
        // Arrange
        var post1 = DataGenerator.GetPostFaker().Generate();
        var post2 = DataGenerator.GetPostFaker().Generate();
        _repository.AddPost(post1);
        _repository.AddPost(post2);

        var commentsPost1 = DataGenerator.GetCommentFaker(post1.Id).Generate(5);
        foreach (var c in commentsPost1) _repository.AddComment(c);

        var commentsPost2 = DataGenerator.GetCommentFaker(post2.Id).Generate(2);
        foreach (var c in commentsPost2) _repository.AddComment(c);

        // Act
        var resultsForPost1 = _repository.GetCommentsByPostId(post1.Id);

        // Assert
        Assert.AreEqual(5, resultsForPost1.Count());
    }

    #endregion

    #region 4. Testy Walidacji (Negatywne)

    [TestMethod]
    [ExpectedException(typeof(DbUpdateException))]
    public void AddPost_NullAuthor_ThrowsDbUpdateException()
    {
        // Arrange
        var invalidPost = new Post
        {
            Author = null!,
            Content = "Test content"
        };

        // Act
        _repository.AddPost(invalidPost);
    }

    [TestMethod]
    [ExpectedException(typeof(DbUpdateException))]
    public void AddComment_NullContent_ThrowsDbUpdateException()
    {
        // Arrange
        var post = DataGenerator.GetPostFaker().Generate();
        _repository.AddPost(post);

        var invalidComment = new Comment
        {
            PostId = post.Id,
            Content = null!
        };

        // Act
        _repository.AddComment(invalidComment);
    }

    #endregion

    #region 5. Test Integracyjny

    [TestMethod]
    public void DeletePost_CascadeDeleteComments()
    {
        // Arrange
        var post = DataGenerator.GetPostFaker().Generate();
        _repository.AddPost(post);

        var comments = DataGenerator.GetCommentFaker(post.Id).Generate(3);
        foreach (var comment in comments)
        {
            _repository.AddComment(comment);
        }

        Assert.AreEqual(3, _context.Comments.Count());

        // Act
        _repository.DeletePost(post);

        // Assert
        Assert.AreEqual(0, _context.Posts.Count());
        Assert.AreEqual(0, _context.Comments.Count());
    }

    #endregion
}