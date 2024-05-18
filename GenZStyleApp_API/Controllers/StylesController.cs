using GenZStyleAPP.BAL.DTOs.Comments;
using GenZStyleAPP.BAL.DTOs.Posts;
using GenZStyleAPP.BAL.DTOs.Styles;
using GenZStyleAPP.BAL.Repository.Interfaces;
using GenZStyleAPP.BAL.Validators.Comments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using ProjectParticipantManagement.BAL.Exceptions;
using ProjectParticipantManagement.BAL.Heplers;

namespace GenZStyleApp_API.Controllers
{
    public class StylesController : ODataController
    {
        public IStyleRepository _styleRepository { get; set; }

        public StylesController(IStyleRepository styleRepository )
        {
            _styleRepository = styleRepository;
        }


        #region Create Post
        [HttpPost("odata/Post/AddNewStyle")]
        [EnableQuery]
        //[PermissionAuthorize("Staff")]
        public async Task<IActionResult> Post(int accountId, [FromForm] GetStyleRequest getStyleRequest)
        {
            try
            {
                /*var resultValid = await _styleRepository.ValidateAsync(addPostRequest);
                if (!resultValid.IsValid)
                {
                    string error = ErrorHelper.GetErrorsString(resultValid);
                    throw new BadRequestException(error);
                }*/
                await this._styleRepository.CreateStyleAsync(accountId, getStyleRequest);
                return Ok(new
                {
                    Status = "Add Style Success"
                    //Data = Created(post),

                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        #endregion
    }
}
