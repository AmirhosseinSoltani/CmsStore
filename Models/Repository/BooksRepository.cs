﻿using BookShop.Classes;
using BookShop.Models.UnitOfWork;
using BookShop.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BookShop.Models.Repository
{
    public class BooksRepository : IBooksRepository
    {
        private readonly BookShopContext _context;
        private readonly IConvertDate _convertDate;
        private readonly IUnitOfWork _UW;
        public BooksRepository(IUnitOfWork UW, IConvertDate convertDate)
        {
            _context = UW._Context;
            _convertDate = convertDate;
            _UW = UW;
        }  


        public List<TreeViewCategory> GetAllCategories()
        {
            var Categories = (from c in _context.Categories
                              where (c.ParentCategoryID == null)
                              select new TreeViewCategory { id = c.CategoryID, title = c.CategoryName }).ToList();
            foreach (var item in Categories)
            {
                BindSubCategories(item);
            }

            return Categories;
        }

        public void BindSubCategories(TreeViewCategory category)
        {
            var SubCategories = (from c in _context.Categories
                                 where (c.ParentCategoryID == category.id)
                                 select new TreeViewCategory { id = c.CategoryID, title = c.CategoryName }).ToList();
            foreach(var item in SubCategories)
            {
                BindSubCategories(item);
                category.subs.Add(item);
            }
        }

        public List<BooksIndexViewModel> GetAllBooks(string title,string ISBN,string Language,string Publisher,string Author,string Translator,string Category)
        {
            string AuthersName = "";
            string TranslatorName = "";
            string CategotyName = "";
            List<BooksIndexViewModel> ViewModel = new List<BooksIndexViewModel>();
            var Books = (from u in _context.Author_Books.Include(b => b.Book).ThenInclude(p => p.Publisher)
                         .Include(a => a.Author) 
                         join l in _context.Languages on u.Book.LanguageID equals l.LanguageID
                         join s in _context.Book_Translators on u.Book.BookID equals s.BookID into bt
                         from bts in bt.DefaultIfEmpty()
                         join t in _context.Translator on bts.TranslatorID equals t.TranslatorID into tr
                         from trl in tr.DefaultIfEmpty()
                         join r in _context.Book_Categories on u.Book.BookID equals r.BookID into bc
                         from bct in bc.DefaultIfEmpty()
                         join c in _context.Categories on bct.CategoryID equals c.CategoryID into cg
                         from cog in cg.DefaultIfEmpty()
                         where (u.Book.Title.Contains(title.TrimStart().TrimEnd())
                         && u.Book.ISBN.Contains(ISBN.TrimStart().TrimEnd())
                         && EF.Functions.Like(l.LanguageName,"%"+Language+"%")
                         && u.Book.Publisher.PublisherName.Contains(Publisher.TrimStart().TrimEnd()))
                         select new
                         {
                             Author = u.Author.FirstName + " " + u.Author.LastName,
                             Translator=trl!=null?trl.Name+" "+trl.Family :"",
                             Category=cog!=null ? cog.CategoryName : "",
                             u.Book.BookID,
                             u.Book.ISBN,
                             u.Book.IsPublish,
                             u.Book.Price,
                             u.Book.PublishDate,
                             u.Book.Publisher.PublisherName,
                             u.Book.Stock,
                             u.Book.Title,
                             l.LanguageName,
                         }).Where(a=>a.Author.Contains(Author.TrimStart().TrimEnd()) && a.Translator.Contains(Translator.TrimStart().TrimEnd())&& a.Category.Contains(Category.TrimStart().TrimEnd()))
                         .GroupBy(b => b.BookID).Select(g => new { BookID = g.Key, BookGroups = g }).ToList();

            foreach (var item in Books)
            {
                AuthersName = "";
                TranslatorName = "";
                CategotyName = "";
                foreach (var a in item.BookGroups.Select(a=>a.Author).Distinct())
                {
                    if (AuthersName == "")
                        AuthersName = a;
                    else
                        AuthersName = AuthersName + " - " + a;
                }

                foreach (var a in item.BookGroups.Select(a => a.Translator).Distinct())
                {
                    if (TranslatorName == "")
                        TranslatorName = a;
                    else
                        TranslatorName = TranslatorName + " - " + a;
                }

                foreach (var a in item.BookGroups.Select(a => a.Category).Distinct())
                {
                    if (CategotyName == "")
                        CategotyName = a;
                    else
                        CategotyName = CategotyName + " - " + a;
                }

                BooksIndexViewModel VM = new BooksIndexViewModel()
                {
                    Author = AuthersName,
                    BookID = item.BookID,
                    ISBN = item.BookGroups.First().ISBN,
                    Title = item.BookGroups.First().Title,
                    Price = item.BookGroups.First().Price,
                    IsPublish = item.BookGroups.First().IsPublish==true?"منتشر شده":"پیش نویس",
                    PublishDate = item.BookGroups.First().PublishDate!=null?_convertDate.ConvertMiladiToShamsi((DateTime)item.BookGroups.First().PublishDate, "dddd d MMMM yyyy ساعت HH:mm:ss"):"",
                    PublisherName = item.BookGroups.First().PublisherName,
                    Stock = item.BookGroups.First().Stock,
                    Language=item.BookGroups.First().LanguageName,
                    Category=CategotyName,
                    Translator=TranslatorName,
                };

                ViewModel.Add(VM);
            }

            return ViewModel;
        }

        public async Task<bool> CreateBookAsync(BooksCreateEditViewModel ViewModel)
        {
            try
            {
                byte[] Image = null;
                if (!string.IsNullOrWhiteSpace(ViewModel.ImageBase64))
                {
                    Image = Convert.FromBase64String(ViewModel.ImageBase64);
                }
                List<Book_Translator> translators = new List<Book_Translator>();
                List<Book_Category> categories = new List<Book_Category>();
                if (ViewModel.TranslatorID != null)
                    translators = ViewModel.TranslatorID.Select(a => new Book_Translator { TranslatorID = a }).ToList();
                if (ViewModel.CategoryID != null)
                    categories = ViewModel.CategoryID.Select(a => new Book_Category { CategoryID = a }).ToList();

                DateTime? PublishDate = null;
                if (ViewModel.IsPublish == true)
                {
                    PublishDate = DateTime.Now;
                }
                Book book = new Book()
                {
                    ISBN = ViewModel.ISBN,
                    IsPublish = ViewModel.IsPublish,
                    NumOfPages = ViewModel.NumOfPages,
                    Stock = ViewModel.Stock,
                    Price = ViewModel.Price,
                    LanguageID = ViewModel.LanguageID,
                    Summary = ViewModel.Summary,
                    Title = ViewModel.Title,
                    Image = Image,
                    PublishYear = ViewModel.PublishYear,
                    PublishDate = PublishDate,
                    Weight = ViewModel.Weight,
                    PublisherID = ViewModel.PublisherID,
                    Author_Books = ViewModel.AuthorID.Select(a => new Author_Book { AuthorID = a }).ToList(),
                    book_Tranlators = translators,
                    book_Categories = categories,
                    File = ViewModel.FileName,
                };

                if(ViewModel.Image!=null)
                {
                    using (var memorySteam = new MemoryStream())
                    {
                        string FileExtension = Path.GetExtension(ViewModel.Image.FileName);
                        await ViewModel.Image.CopyToAsync(memorySteam);
                        var types = FileExtentions.FileType.Image;
                        bool result = FileExtentions.IsValidFile(memorySteam.ToArray(), types, FileExtension.Replace('.', ' '));
                        if(result)
                            book.Image = memorySteam.ToArray();
                    }
                }

                await _UW.BaseRepository<Book>().CreateAsync(book);
                await _UW.Commit();

                return true;
            }

            catch
            {
                return false;
            }
        }

        public async Task<EntityOperationResult> EditBookAsync(BooksCreateEditViewModel ViewModel)
        {
            try
            {
                var Book =await _UW.BaseRepository<Book>().FindByIDAsync(ViewModel.BookID);
                if(Book!=null)
                {
                    DateTime? PublishDate;
                    if (ViewModel.IsPublish == true && Book.IsPublish == false)
                    {
                        PublishDate = DateTime.Now;
                    }
                    else if (Book.IsPublish == true && ViewModel.IsPublish == false)
                    {
                        PublishDate = null;
                    }

                    else
                    {
                        PublishDate = Book.PublishDate;
                    }

                    Book.BookID = ViewModel.BookID;
                    Book.BookID = ViewModel.BookID;
                    Book.Title = ViewModel.Title;
                    Book.ISBN = ViewModel.ISBN;
                    Book.NumOfPages = ViewModel.NumOfPages;
                    Book.Price = ViewModel.Price;
                    Book.Stock = ViewModel.Stock;
                    Book.IsPublish = ViewModel.IsPublish;
                    Book.LanguageID = ViewModel.LanguageID;
                    Book.PublisherID = ViewModel.PublisherID;
                    Book.PublishYear = ViewModel.PublishYear;
                    Book.Summary = ViewModel.Summary;
                    Book.Weight = ViewModel.Weight;
                    Book.PublishDate = PublishDate;
                    Book.File = ViewModel.FileName;
                    Book.Delete = false;

                    var RecentAuthors = (from a in _UW._Context.Author_Books
                                         where (a.BookID == ViewModel.BookID)
                                         select a.AuthorID).ToArray();

                    var RecentTranslators = (from a in _UW._Context.Book_Translators
                                             where (a.BookID == ViewModel.BookID)
                                             select a.TranslatorID).ToArray();

                    var RecentCategories = (from c in _UW._Context.Book_Categories
                                            where (c.BookID == ViewModel.BookID)
                                            select c.CategoryID).ToArray();

                    if (ViewModel.TranslatorID == null)
                        ViewModel.TranslatorID = new int[] { };
                    if (ViewModel.CategoryID == null)
                        ViewModel.CategoryID = new int[] { };

                    var DeletedAuthors = RecentAuthors.Except(ViewModel.AuthorID);
                    var DeletedTranslators = RecentTranslators.Except(ViewModel.TranslatorID);
                    var DeletedCategories = RecentCategories.Except(ViewModel.CategoryID);

                    var AddedAuthors = ViewModel.AuthorID.Except(RecentAuthors);
                    var AddedTranslators = ViewModel.TranslatorID.Except(RecentTranslators);
                    var AddedCategories = ViewModel.CategoryID.Except(RecentCategories);

                    if (DeletedAuthors.Count() != 0)
                        _UW.BaseRepository<Author_Book>().DeleteRange(DeletedAuthors.Select(a => new Author_Book { AuthorID = a, BookID = ViewModel.BookID }).ToList());

                    if (DeletedTranslators.Count() != 0)
                        _UW.BaseRepository<Book_Translator>().DeleteRange(DeletedTranslators.Select(a => new Book_Translator { TranslatorID = a, BookID = ViewModel.BookID }).ToList());

                    if (DeletedCategories.Count() != 0)
                        _UW.BaseRepository<Book_Category>().DeleteRange(DeletedCategories.Select(a => new Book_Category { CategoryID = a, BookID = ViewModel.BookID }).ToList());

                    if (AddedAuthors.Count() != 0)
                        await _UW.BaseRepository<Author_Book>().CreateRangeAsync(AddedAuthors.Select(a => new Author_Book { AuthorID = a, BookID = ViewModel.BookID }).ToList());

                    if (AddedTranslators.Count() != 0)
                        await _UW.BaseRepository<Book_Translator>().CreateRangeAsync(AddedTranslators.Select(a => new Book_Translator { TranslatorID = a, BookID = ViewModel.BookID }).ToList());

                    if (AddedCategories.Count() != 0)
                        await _UW.BaseRepository<Book_Category>().CreateRangeAsync(AddedCategories.Select(a => new Book_Category { CategoryID = a, BookID = ViewModel.BookID }).ToList());

                    await _UW.Commit();

                    return new EntityOperationResult(true, null);
                }
                else
                    return new EntityOperationResult(false, new List<string>() {"کتابی یافت نشد !!!"});
            }

            catch(Exception exp)
            {
                //return new EntityOperationResult(false, new List<string>() { exp.Message });
                return new EntityOperationResult(false, new List<string>() {"در انجام عملیات خطایی رخ داده است."});
            }
        }

        public async Task<UploadFileResult> UploadFileAsync(IFormFile file,string path)
        {
            string FileExtension = Path.GetExtension(file.FileName);
            var types = FileExtentions.FileType.PDF;
            bool result = true;
            using (var memory = new MemoryStream())
            {
                await file.CopyToAsync(memory);
                result = FileExtentions.IsValidFile(memory.ToArray(), types, FileExtension.Replace('.', ' '));
                if (result)
                {
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    return new UploadFileResult(true, null);
                }
                else
                    return new UploadFileResult(false, new List<string>() { "فایل انتخاب شده معتبر نمی باشد." });
            }
        }


        public string CheckFileName(string fileName)
        {
            string FileExtension = Path.GetExtension(fileName);
            int FileNameCount = _UW.BaseRepository<Book>().FindByConditionAsync(f => f.File == fileName).Result.Count();
            int j = 1;
            while (FileNameCount != 0)
            {
                fileName = fileName.Replace(FileExtension, "") + j + FileExtension;
                FileNameCount = _UW.BaseRepository<Book>().FindByConditionAsync(f => f.File == fileName).Result.Count();
                j++;
            }

            return fileName;
        }
    }
}
