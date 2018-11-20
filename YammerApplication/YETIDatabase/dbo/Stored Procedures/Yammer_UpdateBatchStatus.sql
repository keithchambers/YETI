CREATE PROCEDURE Yammer_UpdateBatchStatus (@batchNo int, @status nvarchar(300),@TimesTried int,@ItemCount int)
AS
BEGIN	
	if(@status='Failed')
	begin
		declare @count int
		select @count = ISNULL(TimesTried,0) from YM_SPBatchLists where BatchNo = @batchNo
		if(@count < @TimesTried)
		begin
			Update YM_SPBatchLists set status = null,TimesTried = @count+1,ItemCount = @ItemCount where BatchNo = @batchNo
		end
		else
		begin
			Update YM_SPBatchLists set status = @status where BatchNo = @batchNo	
		end
	end
	else
	begin
		Update YM_SPBatchLists set status = @status where BatchNo = @batchNo	
	end
END 
