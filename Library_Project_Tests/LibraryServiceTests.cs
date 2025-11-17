using Library_Project.Model;
using Library_Project.Services;
using Library_Project.Services.Interfaces;
using Moq;

namespace Library_Project_Tests
{
    public class LibraryServiceTests
    {
        private readonly Mock<IBookRepository> _repoMock;
        private readonly Mock<IMemberService> _memberMock;
        private readonly Mock<INotificationService> _notifMock;
        private readonly LibraryService _service;

        public LibraryServiceTests()
        {
            _repoMock = new Mock<IBookRepository>();
            _memberMock = new Mock<IMemberService>();
            _notifMock = new Mock<INotificationService>();

            _service = new LibraryService(_repoMock.Object, _memberMock.Object, _notifMock.Object);
        }

        ///<summary>
        ///Validates that the AddBook method adds a new book if a book with that title does not already exist.
        ///</summary>
        [Fact]
        public void AddBook_ShouldAddNewBook_WhenNotExists()
        {
            //Arrange
            string bookTitle = "1984";
            int copiesAmount = 3;
            _repoMock.Setup(r => r.FindBook(bookTitle)).Returns((Book)null);

            //Act
            _service.AddBook(bookTitle, copiesAmount);

            //Assert
            _repoMock.Verify(r => r.SaveBook(It.Is<Book>(b => b.Title == bookTitle && b.Copies == copiesAmount)), Times.Once);
        }

        ///<summary>
        ///Validates that the AddBook method increases the copy count if a book with that title already exists.
        ///</summary>
        [Fact]
        public void AddBook_ShouldIncreaseCopies_WhenBookExists()
        {
            //Arrange
            string bookTitle = "1984";
            int initialCopies = 2;
            int addedCopies = 3;
            int expectedCopies = initialCopies + addedCopies;
            var existingBook = new Book { Title = bookTitle, Copies = initialCopies };
            _repoMock.Setup(r => r.FindBook(bookTitle)).Returns(existingBook);

            //Act
            _service.AddBook(bookTitle, addedCopies);

            //Assert
            Assert.Equal(expectedCopies, existingBook.Copies);
            _repoMock.Verify(r => r.SaveBook(existingBook), Times.Once);
        }

        ///<summary>
        ///Tests that the AddBook method throws an ArgumentException when the title is null, empty, or whitespace.
        ///</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void AddBook_ShouldThrowArgumentException_WhenTitleIsInvalid(string invalidTitle)
        {
            //Arrange
            int validCopies = 5;

            //Act and Assert
            var exception = Assert.Throws<ArgumentException>(
                () => _service.AddBook(invalidTitle, validCopies)
            );
        }

        ///<summary>
        ///Tests that the AddBook method throws an ArgumentException when the copies count is zero or negative.
        ///</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void AddBook_ShouldThrowArgumentException_WhenCopiesIsInvalid(int invalidCopies)
        {
            //Arrange
            string bookTitle = "1984";

            //Act and Assert
            var exception = Assert.Throws<ArgumentException>(
                () => _service.AddBook(bookTitle, invalidCopies)
            );
        }

        ///<summary>
        ///Validates that the BorrowBook method decreases the copy count when the member is valid and the book is available.
        /// </summary>
        [Fact]
        public void BorrowBook_ShouldDecreaseCopies_WhenValidMemberAndAvailable()
        {
            //Arrange
            string bookTitle = "1984";
            int copiesAmount = 2;
            int memberId = 1;
            var book = new Book { Title = bookTitle, Copies = copiesAmount };
            _repoMock.Setup(r => r.FindBook(bookTitle)).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(memberId)).Returns(true);

            //Act
            bool result = _service.BorrowBook(memberId, bookTitle);

            //Assert
            Assert.True(result);
            Assert.Equal(1, book.Copies);
            _notifMock.Verify(n => n.NotifyBorrow(memberId, bookTitle), Times.Once);
        }

        ///<summary>
        ///Validates that BorrowBook returns false if the book exists but has no available copies.
        ///</summary>
        [Fact]
        public void BorrowBook_ShouldReturnFalse_WhenNoCopies()
        {
            //Arrange
            string bookTitle = "1984";
            int memberId = 1;
            int initialCopies = 0;
            var book = new Book { Title = bookTitle, Copies = initialCopies };

            _repoMock.Setup(r => r.FindBook(bookTitle)).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(memberId)).Returns(true);

            //Act
            bool result = _service.BorrowBook(memberId, bookTitle);

            //Assert
            Assert.False(result);
            Assert.Equal(initialCopies, book.Copies);
            _notifMock.Verify(n => n.NotifyBorrow(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        ///<summary>
        ///Validates that BorrowBook throws an InvalidOperationException if the memberId is invalid.
        ///</summary>
        [Fact]
        public void BorrowBook_ShouldThrow_WhenInvalidMember()
        {
            //Arrange
            int invalidMemberId = 1;
            string bookTitle = "1984";
            _memberMock.Setup(m => m.IsValidMember(invalidMemberId)).Returns(false);

            //Act and Assert
            Assert.Throws<InvalidOperationException>(() => _service.BorrowBook(invalidMemberId, bookTitle));
            _repoMock.Verify(r => r.FindBook(It.IsAny<string>()), Times.Never);
        }

        ///<summary>
        ///Verifies that the copy count is different from the initial value after a successful borrow.
        /// </summary>
        [Fact]
        public void BorrowBook_ShouldChangeCopies_WhenSuccessful()
        {
            //Arrange
            string bookTitle = "1984";
            int memberId = 1;
            int initialCopies = 5;
            int expectedCopies = initialCopies - 1;
            var book = new Book { Title = bookTitle, Copies = initialCopies };

            _repoMock.Setup(r => r.FindBook(bookTitle)).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(memberId)).Returns(true);

            //Act
            _service.BorrowBook(memberId, bookTitle);

            //Assert
            Assert.NotEqual(initialCopies, book.Copies);
            Assert.Equal(expectedCopies, book.Copies);
        }

        ///<summary>
        ///Verifies that the SaveBook method is called exactly one time after a successful borrow.
        ///</summary>
        [Fact]
        public void BorrowBook_ShouldCallSaveExactlyOnce_WhenSuccessful()
        {
            //Arrange
            string bookTitle = "1984";
            int memberId = 1;
            int initialCopies = 2;
            var book = new Book { Title = bookTitle, Copies = initialCopies };

            _repoMock.Setup(r => r.FindBook(bookTitle)).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(memberId)).Returns(true);

            //Act
            _service.BorrowBook(memberId, bookTitle);

            //Assert
            _repoMock.Verify(r => r.SaveBook(book), Times.Exactly(1));
        }

        ///<summary>
        ///Validates that BorrowBook returns false if the book is not found in the repository.
        ///</summary>
        [Fact]
        public void BorrowBook_ShouldReturnFalse_WhenBookNotFound()
        {
            //Arrange
            string bookTitle = "Unknown";
            int memberId = 1;

            _repoMock.Setup(r => r.FindBook(bookTitle)).Returns((Book)null);
            _memberMock.Setup(m => m.IsValidMember(memberId)).Returns(true);

            //Act
            bool result = _service.BorrowBook(memberId, bookTitle);

            //Assert
            Assert.False(result);
            _repoMock.Verify(r => r.SaveBook(It.IsAny<Book>()), Times.Never);
            _notifMock.Verify(n => n.NotifyBorrow(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        ///<summary>
        ///Validates that borrowing the last available copy sets the copy count to zero.
        ///</summary>
        [Fact]
        public void BorrowBook_ShouldSetCopiesToZero_WhenBorrowingLastCopy()
        {
            //Arrange
            string bookTitle = "1984";
            int memberId = 1;
            int initialCopies = 1;
            var book = new Book { Title = bookTitle, Copies = initialCopies };

            _repoMock.Setup(r => r.FindBook(bookTitle)).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(memberId)).Returns(true);

            //Act
            bool result = _service.BorrowBook(memberId, bookTitle);

            //Assert
            Assert.True(result);
            Assert.Equal(0, book.Copies);
            _repoMock.Verify(r => r.SaveBook(book), Times.Once);
            _notifMock.Verify(n => n.NotifyBorrow(memberId, bookTitle), Times.Once);
        }

        ///<summary>
        ///Validates that the ReturnBook method increases the copy count and sends a return notification.
        ///</summary>
        [Fact]
        public void ReturnBook_ShouldIncreaseCopies()
        {
            //Arrange
            string bookTitle = "1984";
            int memberId = 1;
            int initialCopies = 1;
            int expectedCopies = initialCopies + 1;
            var book = new Book { Title = bookTitle, Copies = initialCopies };

            _repoMock.Setup(r => r.FindBook(bookTitle)).Returns(book);

            //Act
            bool result = _service.ReturnBook(memberId, bookTitle);

            //Assert
            Assert.True(result);
            Assert.Equal(expectedCopies, book.Copies);
            _notifMock.Verify(n => n.NotifyReturn(memberId, bookTitle), Times.Once);
        }

        ///<summary>
        ///Validates that ReturnBook returns false if a book with the specified title is not found in the repository.
        ///</summary>
        [Fact]
        public void ReturnBook_ShouldReturnFalse_WhenBookNotFound()
        {
            //Arrange
            string unknownBookTitle = "Unknown";
            int memberId = 1;
            _repoMock.Setup(r => r.FindBook(unknownBookTitle)).Returns((Book)null);

            //Act
            bool result = _service.ReturnBook(memberId, unknownBookTitle);

            //Assert
            Assert.False(result);
            _notifMock.Verify(n => n.NotifyReturn(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        ///<summary>
        ///Validates that ReturnBook succeeds even if the memberId is not valid.
        ///</summary>
        [Fact]
        public void ReturnBook_ShouldSucceed_EvenWhenMemberIsInvalid()
        {
            //Arrange
            string bookTitle = "1984";
            int invalidMemberId = 9;
            int initialCopies = 1;
            var book = new Book { Title = bookTitle, Copies = initialCopies };
            _repoMock.Setup(r => r.FindBook(bookTitle)).Returns(book);

            //Act
            bool result = _service.ReturnBook(invalidMemberId, bookTitle);

            //Assert
            Assert.True(result);
            Assert.Equal(initialCopies + 1, book.Copies);
            _memberMock.Verify(m => m.IsValidMember(It.IsAny<int>()), Times.Never);
            _notifMock.Verify(n => n.NotifyReturn(invalidMemberId, bookTitle), Times.Once);
        }

        ///<summary>
        ///Validates that GetAvailableBooks returns a list containing only books where the copy count is greater than zero.
        ///</summary>
        [Fact]
        public void GetAvailableBooks_ShouldReturnOnlyBooksWithCopies()
        {
            //Arrange
            string unavailableBookTitle = "Book A";
            string availableBookTitle1 = "Book B";
            string availableBookTitle2 = "Book C";
            int copiesAmount = 30;
            var allBooks = new List<Book>
            {
                new Book { Title = unavailableBookTitle, Copies = 0 },
                new Book { Title = availableBookTitle1, Copies = copiesAmount },
                new Book { Title = availableBookTitle2, Copies = copiesAmount }
            };
            int expectedCount = 2;
            _repoMock.Setup(r => r.GetAllBooks()).Returns(allBooks);

            //Act
            var available = _service.GetAvailableBooks();

            //Assert
            Assert.NotEmpty(available);
            Assert.Equal(expectedCount, available.Count);
            Assert.Contains(available, b => b.Title == availableBookTitle1);
            Assert.Contains(available, b => b.Title == availableBookTitle2);
        }

        ///<summary>
        ///Validates that GetAvailableBooks returns an empty list if all books in the repository have 0 copies.
        /// </summary>
        [Fact]
        public void GetAvailableBooks_ShouldReturnEmpty_WhenNoBooksAvailable()
        {
            //Arrange
            var allBooks = new List<Book>
            {
                new Book { Title = "Book A", Copies = 0 },
                new Book { Title = "Book B", Copies = 0 }
            };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(allBooks);

            //Act
            var result = _service.GetAvailableBooks();

            //Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        ///<summary>
        /// Verifies that GetAvailableBooks returns a non-null, empty list when the repository returns an empty list.
        ///</summary>
        [Fact]
        public void GetAvailableBooks_ShouldReturnList_WhenRepositoryIsEmpty()
        {
            //Arrange
            var emptyBookList = new List<Book>();
            _repoMock.Setup(r => r.GetAllBooks()).Returns(emptyBookList);

            //Act
            var result = _service.GetAvailableBooks();

            //Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        ///<summary>
        ///Validates that GetAvailableBooks correctly filters out books with no copies.
        ///</summary>
        [Fact]
        public void GetAvailableBooks_ShouldNotReturnBooks_WithZeroCopies()
        {
            //Arrange
            var availableBook = new Book { Title = "Available Book", Copies = 1 };
            var unavailableBook = new Book { Title = "Unavailable Book", Copies = 0 };
            var allBooks = new List<Book> { availableBook, unavailableBook };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(allBooks);

            //Act
            var result = _service.GetAvailableBooks();

            //Assert
            Assert.DoesNotContain(unavailableBook, result);
        }

    }
}
