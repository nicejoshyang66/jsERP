

# 規格說明

1. 以下是關於左側 menu 選單的規格 
2. 先參考 d:\myPorgram5\xxx_Backup 下 controller: zsysprogram 下相關的功能
   A. 畫面參考 controller: zsysprogram,
   b. master -> 
        select form_id,sort_no, remark
        from sys_program where isnull(parent_id,'')='' and is_enabled=1
   c. detail->
        select form_id, form_caption,sort_no,controller_name, action_nme, route_name
        from sys_program where parent_id = master->form_id   

3. 參考 agents.md                 

       

