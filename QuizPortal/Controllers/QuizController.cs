using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizPortal.Helper;
using QuizPortal.Models;
using QuizPortal.Models.Dtos;
using QuizPortal.Proxies;
using QuizPortal.Repositories;

namespace QuizPortal.Controllers
{
    public class QuizController : Controller
    {
        private readonly IWiredProxy _wiredProxy;
        private readonly IRepositoryFactory _repositoryFactory;
        private readonly IMapper _mapper;

        public QuizController(IWiredProxy wiredProxy, IRepositoryFactory repositoryFactory, IMapper mapper)
        {
            _wiredProxy = wiredProxy;
            _repositoryFactory = repositoryFactory;
            _mapper = mapper;
        }

        [BindProperty]
        public CreateQuizViewDto CreateQuizViewDto { get; set; }

        public async Task<IActionResult> CreateQuiz()
        {
            if (HttpContext.Session.GetString(Constants.SessionUserId) == null)
            {
                return Redirect(Url.Action("Login", "User"));
            }

            var articleList = await _wiredProxy.GetLastFiveArticlesAsync();

            CreateQuizViewDto = new CreateQuizViewDto();
            CreateQuizViewDto.ArticleList = articleList.ToList();

            return View(CreateQuizViewDto);
        }

        [HttpPost]
        [ActionName("CreateQuiz")]
        public async Task<IActionResult> CreateQuizPost()
        {
            CreateQuizViewDto.ErrorMessage = null;

            if (ModelState.IsValid)
            {
                
                if (CreateQuizViewDto.QuestionArr.Select(q => q.QuestionText).Distinct().Count() != 4)
                {
                    CreateQuizViewDto.ErrorMessage = "Pyetjet duhet të jenë unike";

                    return View(CreateQuizViewDto);
                }

                
                foreach(var q in CreateQuizViewDto.QuestionArr)
                {
                    if(q.AnswerA == q.AnswerB ||
                        q.AnswerA == q.AnswerC ||
                        q.AnswerA == q.AnswerD ||
                        q.AnswerB == q.AnswerC ||
                        q.AnswerB == q.AnswerD ||
                        q.AnswerC == q.AnswerD)
                    {
                        CreateQuizViewDto.ErrorMessage = "Një pyetje nuk mund të ketë të njëjtën përgjigje më shumë se një herë";

                        return View(CreateQuizViewDto);
                    }
                }

                var transaction = await _repositoryFactory.BeginTransactionAsync();

                var quizRepository = _repositoryFactory.GetQuizRepository();

                var selectedArt = CreateQuizViewDto.ArticleList.FirstOrDefault(a => a.ArticleId == CreateQuizViewDto.SelectedArticleId);

                if (selectedArt == null)
                {
                    return View(CreateQuizViewDto);
                }

                var quiz = _mapper.Map<Quiz>(selectedArt);

                await quizRepository.CreateQuizAsync(quiz);
                await _repositoryFactory.SaveAsync();

                var questionRepository = _repositoryFactory.GetQuestionRepository();

                foreach (var item in CreateQuizViewDto.QuestionArr)
                {
                    var ques = _mapper.Map<Question>(item);
                    ques.QuizId = quiz.Id;

                    await questionRepository.CreateQuestionAsync(ques);
                }

                await _repositoryFactory.SaveAsync();

                transaction.Commit();

                return RedirectToAction("Index", "Quiz");
            }

            return View(CreateQuizViewDto);
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString(Constants.SessionUserId) == null)
            {
                return Redirect(Url.Action("Login", "User"));
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuizzes()
        {
            var quizRepository = _repositoryFactory.GetQuizRepository();

            var quizList = await quizRepository.GetAllQuizzesAsync();

            var quizDtoList = _mapper.Map<List<QuizDto>>(quizList.ToList());

            return Json(new { data = quizDtoList });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var quizRepository = _repositoryFactory.GetQuizRepository();

            var quizFromDb = await quizRepository.GetQuizAsync(id);

            if (quizFromDb == null)
            {
                return Json(new { success = false, message = "Gabim gjatë fshirjes" });
            }

            quizRepository.DeleteQuiz(quizFromDb);
            await _repositoryFactory.SaveAsync();

            return Json(new { success = true, message = "Fshirja ishte me sukses" });
        }

        [HttpGet]
        public async Task<IActionResult> Quiz(int id)
        {
            if (HttpContext.Session.GetString(Constants.SessionUserId) == null)
            {
                return Redirect(Url.Action("Login", "User"));
            }

            var quizRepository = _repositoryFactory.GetQuizRepository();
            var questionRepository = _repositoryFactory.GetQuestionRepository();

            var quizFromDb = await quizRepository.GetQuizAsync(id);

            
            if(quizFromDb == null)
            {
                return Redirect(Url.Action("Index", "Quiz"));
            }

            var questionList = await questionRepository.GetAllQuestionsAsync(id);

            var quizDto = _mapper.Map<QuizDto>(quizFromDb);
            var questionDtoList = _mapper.Map<List<QuestionDto>>(questionList);

            var quizViewDto = new QuizViewDto();

            quizViewDto.QuizDto = quizDto;
            quizViewDto.QuestionDtoList = questionDtoList;

            return View(quizViewDto);
        }
    }
}
