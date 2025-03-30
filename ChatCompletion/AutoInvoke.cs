using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Linq.Dynamic.Core;

public class LibrAIan 
{
    public LibrAIan()
    {


       
    }
   
    public List<Book> Books { get; set; }
    [KernelFunction("filter_books_by_expression")]
    [Description("Filter the list of books by a given expression. The expression must be a valid c# predicate. like book => book.Name == 'The Great Gatsby'")]
    [return: Description("List of books that match the expression")]
    public List<Book> FilterBooksByExpression(string predicate)
    {
        var books = ListAllBooks();
        // Compile the prediicate string into a lambda expression
        var parameter = Expression.Parameter(typeof(Book), "book");
        var lambda = DynamicExpressionParser.ParseLambda(new[] { parameter }, null, predicate);
        //Apply the predicate
        var filteredBooks = books.AsQueryable().Where((Expression<Func<Book, bool>>)lambda).ToList();
        return filteredBooks;
    }

    private List<Book> ListAllBooks()
    {
        Books = new List<Book>()
        {
            new Book()
            {
                Name = "The Great Gatsby",
                Author = "F. Scott Fitzgerald",
                YearPublished = 1925,
                Location = "Shelf A1",
                Available = true,
                Genre = Genre.Fiction
            },
            new Book()
            {
                Name = "1984",
                Author = "George Orwell",
                YearPublished = 1949,
                Location = "Shelf B2",
                Available = false,
                Genre = Genre.Science
            },
            new Book()
            {
                Name = "To Kill a Mockingbird",
                Author = "Harper Lee",
                YearPublished = 1960,
                Location = "Shelf C3",
                Available = true,
                Genre = Genre.History
            },
            new Book()
{
    Name = "American Psycho",
    Author = "Bret Easton Ellis",
    YearPublished = 1991,
    Location = "Shelf D4",
    Available = true,
    Genre = Genre.Thriller
},
new Book()
{
    Name = "The Girl with the Dragon Tattoo",
    Author = "Stieg Larsson",
    YearPublished = 2005,
    Location = "Shelf B2",
    Available = true,
    Genre = Genre.Mystery
},
new Book()
{
    Name = "The Road",
    Author = "Cormac McCarthy",
    YearPublished = 2006,
    Location = "Shelf A1",
    Available = true,
    Genre = Genre.PostApocalyptic
},
new Book()
{
    Name = "Fight Club",
    Author = "Chuck Palahniuk",
    YearPublished = 1996,
    Location = "Shelf E5",
    Available = true,
    Genre = Genre.Satire
},
new Book()
{
    Name = "The Silence of the Lambs",
    Author = "Thomas Harris",
    YearPublished = 1988,
    Location = "Shelf C4",
    Available = true,
    Genre = Genre.Horror
},
new Book()
{
    Name = "1984",
    Author = "George Orwell",
    YearPublished = 1949,
    Location = "Shelf F3",
    Available = true,
    Genre = Genre.Dystopian
},
new Book()
{
    Name = "The Catcher in the Rye",
    Author = "J.D. Salinger",
    YearPublished = 1951,
    Location = "Shelf G1",
    Available = true,
    Genre = Genre.Fiction
},
new Book()
{
    Name = "Brave New World",
    Author = "Aldous Huxley",
    YearPublished = 1932,
    Location = "Shelf H2",
    Available = true,
    Genre = Genre.ScienceFiction
},
new Book()
{
    Name = "Lord of the Flies",
    Author = "William Golding",
    YearPublished = 1954,
    Location = "Shelf I3",
    Available = true,
    Genre = Genre.Allegory
},
new Book()
{
    Name = "The Shining",
    Author = "Stephen King",
    YearPublished = 1977,
    Location = "Shelf J4",
    Available = false,
    Genre = Genre.Horror
}



        };
        return Books;
    }

}

public class Book
{
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int YearPublished { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool Available { get; set; }
    public Genre Genre { get; set; }
    

    [KernelFunction("BookClassProperties")]
    [Description("Get all properties of the book class")]
    [return: Description("List of all properties of the book class")]
    public List<string> GetAllBookProperties()
    {
        Type bookType = typeof(Book);
        var properties = bookType.GetProperties();
        List<string> propertyNames = new List<string>();
        foreach (var property in properties)
        {
            propertyNames.Add(property.Name);
        }
        return propertyNames;
    }
}


public enum Genre
{
    Fiction,
    NonFiction,
    Science,
    History,
    Fantasy,
    Mystery,
    Romance,
    Thriller,
    Horror,
    ScienceFiction,
    Dystopian,
    PostApocalyptic,
    Satire,
    Allegory,
    Biography,
}